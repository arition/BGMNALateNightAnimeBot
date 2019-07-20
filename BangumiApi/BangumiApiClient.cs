﻿using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BangumiApi.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BangumiApi
{
    public class BangumiApiClient : IDisposable
    {
        protected const string BangumiApiUrl = "http://api.bgm.tv";
        protected const string BangumiUrl = "http://bgm.tv";

        protected readonly Regex AnimeTypeRegex =
            new Regex(@"(?<=<small class=""grey"">).+?(?=</small> *</h1>)", RegexOptions.Compiled);

        protected readonly Regex DayOfWeekRegex = new Regex(@"(?<=<li><span class=""tip"">放送星期: </span>).*?(?=</li>)",
            RegexOptions.Compiled);

        protected readonly Regex DescriptionRegex =
            new Regex(
                @"(?<=<div id=""subject_summary"" class=""subject_summary"" property=""v:summary"">).*?(?=</div>)",
                RegexOptions.Singleline | RegexOptions.Compiled);

        protected readonly Regex OfficialHomePageRegex =
            new Regex(@"(?<=<li><span class=""tip"">官方网站: </span>)http://.*?(?=</li>)", RegexOptions.Compiled);

        protected readonly Regex SpecialOnAirDateRegex =
            new Regex(@"(?<=<li><span class=""tip"">(上映年度|发售日): </span>).*?(?=</li>)", RegexOptions.Compiled);

        protected readonly Regex TotalEpisodeRegex =
            new Regex(@"(?<=<li><span class=""tip"">话数: </span>).*?(?=</li>)", RegexOptions.Compiled);

        protected readonly Regex UrlRegex = new Regex(
            @"[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)",
            RegexOptions.Compiled);

        protected HttpClient HttpClient = new HttpClient();

        public void Dispose()
        {
            HttpClient.Dispose();
        }

        public async Task<SearchResultModel> SearchSubjectAsync(SearchSubjectModel searchSubjectModel)
        {
            var httpResponseMessage =
                await
                    HttpClient.GetAsync(BangumiApiUrl + "/search/subject/" + searchSubjectModel.Keyword +
                                        "?max_results=" + searchSubjectModel.MaxCount +
                                        "&start=" + searchSubjectModel.IndexStart + "&type=2");
            httpResponseMessage.EnsureSuccessStatusCode();
            var rawContent = await httpResponseMessage.Content.ReadAsStringAsync();
            if (rawContent.StartsWith("<"))
                return new SearchResultModel
                {
                    Count = 0
                };
            if (ContentContainsError(rawContent)) throw new BangumiApiException(GetContentErrorMessage(rawContent));
            var searchResultModel = JsonConvert.DeserializeObject<SearchResultModel>(rawContent);
            if (searchResultModel.SubjectInfo == null)
                return new SearchResultModel
                {
                    Count = 0
                };
            foreach (var subjectInfo in searchResultModel.SubjectInfo)
                await AddAdditionalInformationAsync(subjectInfo.Id.ToString(), subjectInfo);
            return searchResultModel;
        }

        public async Task<SubjectInfo> GetSubjectAsync(string subjectId)
        {
            var httpResponseMessage = await HttpClient.GetAsync(BangumiApiUrl + "/subject/" + subjectId);
            httpResponseMessage.EnsureSuccessStatusCode();
            var rawContent = await httpResponseMessage.Content.ReadAsStringAsync();
            if (ContentContainsError(rawContent)) throw new BangumiApiException(GetContentErrorMessage(rawContent));
            var subjectInfo = JsonConvert.DeserializeObject<SubjectInfo>(rawContent);
            return await AddAdditionalInformationAsync(subjectId, subjectInfo);
        }

        private async Task<SubjectInfo> AddAdditionalInformationAsync(string subjectId, SubjectInfo subjectInfo)
        {
            var httpResponseMessage = await HttpClient.GetAsync(BangumiUrl + "/subject/" + subjectId);
            httpResponseMessage.EnsureSuccessStatusCode();
            var rawContent = await httpResponseMessage.Content.ReadAsStringAsync();
            var descriptionMatch = DescriptionRegex.Match(rawContent);
            if (descriptionMatch.Success)
                subjectInfo.Description =
                    string.IsNullOrWhiteSpace(descriptionMatch.Value) ? null : descriptionMatch.Value;
            var totalEpisodeMatch = TotalEpisodeRegex.Match(rawContent);
            if (totalEpisodeMatch.Success)
                subjectInfo.TotalEpisode =
                    string.IsNullOrWhiteSpace(totalEpisodeMatch.Value) ? null : totalEpisodeMatch.Value;
            var animeTypeMatch = AnimeTypeRegex.Match(rawContent);
            if (animeTypeMatch.Success)
                subjectInfo.AnimeType = string.IsNullOrWhiteSpace(animeTypeMatch.Value) ? null : animeTypeMatch.Value;
            var officialHomePageMatch = OfficialHomePageRegex.Match(rawContent);
            if (officialHomePageMatch.Success)
            {
                subjectInfo.OfficialHomePage = officialHomePageMatch.Value;
                if (string.IsNullOrWhiteSpace(officialHomePageMatch.Value))
                    subjectInfo.OfficialHomePage = null;
                else if (!Uri.TryCreate(subjectInfo.OfficialHomePage, UriKind.RelativeOrAbsolute, out _))
                    subjectInfo.OfficialHomePage = null;
                else if (UrlRegex.Matches(subjectInfo.OfficialHomePage).Count != 1)
                    subjectInfo.OfficialHomePage = null;
            }

            var specialOnAirDateMatch = SpecialOnAirDateRegex.Match(rawContent);
            if (specialOnAirDateMatch.Success)
            {
                if (DateTime.TryParse(specialOnAirDateMatch.Value, out var onAirDate)) subjectInfo.StartDate = onAirDate;
            }

            var dayOfWeekMatch = DayOfWeekRegex.Match(rawContent);
            if (dayOfWeekMatch.Success)
                switch (dayOfWeekMatch.Value)
                {
                    case "星期日":
                        subjectInfo.DayOfWeek = DayOfWeek.Sunday;
                        break;
                    case "星期一":
                        subjectInfo.DayOfWeek = DayOfWeek.Monday;
                        break;
                    case "星期二":
                        subjectInfo.DayOfWeek = DayOfWeek.Tuesday;
                        break;
                    case "星期三":
                        subjectInfo.DayOfWeek = DayOfWeek.Wednesday;
                        break;
                    case "星期四":
                        subjectInfo.DayOfWeek = DayOfWeek.Thursday;
                        break;
                    case "星期五":
                        subjectInfo.DayOfWeek = DayOfWeek.Friday;
                        break;
                    case "星期六":
                        subjectInfo.DayOfWeek = DayOfWeek.Saturday;
                        break;
                }

            return subjectInfo;
        }

        #region help code

        protected static bool ContentContainsError(string rawContent)
        {
            return /*rawContent.StartsWith("<") ||*/ JObject.Parse(rawContent)["error"] != null;
        }

        protected static string GetContentErrorMessage(string rawContent)
        {
            return rawContent.StartsWith("<")
                ? "Error information is Html Format, cannot purse"
                : JObject.Parse(rawContent)["error"].ToString();
        }

        #endregion
    }
}
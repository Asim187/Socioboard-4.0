﻿using Api.Socioboard.Model;
using Domain.Socioboard.Models;
using Facebook;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Socioboard.Facebook.Data;
using Api.Socioboard.Controllers;

namespace Api.Socioboard.Helper
{
    public static class FacebookHelper
    {
        
        public static string ComposeMessage(Domain.Socioboard.Enum.FbProfileType profiletype, string accessToken, string fbUserId, string message, string profileId, long userId, string imagePath, string link, DatabaseRepository dbr, ILogger _logger)
        {
          
            string ret = "";

            FacebookClient fb = new FacebookClient();
            fb.AccessToken = accessToken;
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls;
            var args = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(message))
            {
                args["message"] = message;
            }
            if (profiletype == Domain.Socioboard.Enum.FbProfileType.FacebookProfile)
            {
                args["privacy"] = FbUser.SetPrivacy("Public", fb, profileId);
            }
            try
            {
                if (string.IsNullOrEmpty(link))
                {
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        Uri u = new Uri(imagePath);
                        string filename = string.Empty;
                        string extension = string.Empty;
                        extension = System.IO.Path.GetExtension(u.AbsolutePath).Replace(".", "");
                        var media = new FacebookMediaObject
                        {
                            FileName = "filename",
                            ContentType = "image/" + extension
                        };
                        //byte[] img = System.IO.File.ReadAllBytes(imagepath);
                        var webClient = new WebClient();
                        byte[] img = webClient.DownloadData(imagePath);
                        media.SetValue(img);
                        args["source"] = media;
                        ret = fb.Post("v2.7/" + fbUserId + "/photos", args).ToString();//v2.1
                    }
                    else
                    {
                        args["message"] = message;
                        ret = fb.Post("v2.7/" + fbUserId + "/feed", args).ToString();
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(link))
                    {
                        args["link"] = link;
                    }
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        args["picture"] = imagePath.Replace("&amp;", "&");
                    }
                    ret = fb.Post("v2.7/" + fbUserId + "/feed", args).ToString();//v2.1

                }
                ScheduledMessage scheduledMessage = new ScheduledMessage();
                scheduledMessage.createTime = DateTime.UtcNow;
                scheduledMessage.picUrl = "https://graph.facebook.com/" + fbUserId + "/picture?type=small";//imagePath;
                scheduledMessage.profileId = profileId;
                scheduledMessage.profileType = Domain.Socioboard.Enum.SocialProfileType.Facebook;
                scheduledMessage.scheduleTime = DateTime.UtcNow;
                scheduledMessage.shareMessage = message;
                scheduledMessage.userId = userId;
                scheduledMessage.status = Domain.Socioboard.Enum.ScheduleStatus.Compleated;
                scheduledMessage.url = imagePath;//"https://graph.facebook.com/"+ fbUserId + "/picture?type=small";
                dbr.Add<ScheduledMessage>(scheduledMessage);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
            }
            return ret;
        }


        public static string FacebookComposeMessageRss(string message, string accessToken, string FbUserId, string title, string link, string rssFeedId, Helper.AppSettings _appSettings)
        {
            string ret = "";
            FacebookClient fb = new FacebookClient();
            MongoRepository rssfeedRepo = new MongoRepository("RssFeed", _appSettings);
            try
            {
                fb.AccessToken = accessToken;
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls;
                var args = new Dictionary<string, object>();
                args["message"] = message;
                args["link"] = link;
                ret = fb.Post("v2.0/" + FbUserId + "/feed", args).ToString();
                var builders = Builders<BsonDocument>.Filter;
                FilterDefinition<BsonDocument> filter = builders.Eq("strId", rssFeedId);
                var update = Builders<BsonDocument>.Update.Set("Status", true);
                rssfeedRepo.Update<Domain.Socioboard.Models.Mongo.RssFeed>(update, filter);

                return ret = "Messages Posted Successfully";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return ret = "Message Could Not Posted";
            }
        }


        public static string GetFbPageDetails(string url, string accestoken)
        {
            try
            {
                FacebookClient fb = new FacebookClient();
                fb.AccessToken = accestoken;
                dynamic pageinfo = null;
                string[] pageurl = url.Split(',');
                string ProfilePageId = "";
                try
                {
                    foreach (var item in pageurl)
                    {
                        pageinfo = fb.Get(item);
                        ProfilePageId = pageinfo["id"] + "," + ProfilePageId;
                    }

                }
                catch (Exception ex1)
                {


                }

                return ProfilePageId;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public static List<Domain.Socioboard.Models.Mongo.PageDetails> GetFbPagePostDetails(string url, string accestoken)
        {
            try
            {
                FacebookClient fb = new FacebookClient();
                fb.AccessToken = accestoken;
                dynamic pageinfo = null;
                string[] pageurl = url.Split(',');
                List<Domain.Socioboard.Models.Mongo.PageDetails> lstPageDetail = new List<Domain.Socioboard.Models.Mongo.PageDetails>();
                try
                {
                    foreach (var item in pageurl)
                    {
                        Domain.Socioboard.Models.Mongo.PageDetails _pagedetail = new Domain.Socioboard.Models.Mongo.PageDetails();
                        pageinfo = fb.Get(item);
                        //ProfilePageId = pageinfo["id"] + "," + ProfilePageId;
                        _pagedetail.PageId = pageinfo["id"];
                        _pagedetail.PageUrl = item;
                        lstPageDetail.Add(_pagedetail);
                    }

                }
                catch (Exception ex1)
                {


                }

                return lstPageDetail;
            }
            catch (Exception ex)
            {
                return new List<Domain.Socioboard.Models.Mongo.PageDetails>();
            }
        }


        public static List<Domain.Socioboard.Models.FacebookGroup> GetAllFacebookGroups(string accessToken, string client_id, string redirect_uri, string client_secret)
        {

            List<Domain.Socioboard.Models.FacebookGroup> lstAddFacebookGroup = new List<Domain.Socioboard.Models.FacebookGroup>();
            FacebookClient fb = new FacebookClient();
            string profileId = string.Empty;
            dynamic output = null;
            if (accessToken != null)
            {
                try
                {
                    fb.AccessToken = accessToken;
                    dynamic profile = fb.Get("v2.7/me");
                    output = fb.Get("v2.7/me/groups");
                    foreach (var item in output["data"])
                    {
                        try
                        {
                            Domain.Socioboard.Models.FacebookGroup objAddFacebookGroup = new Domain.Socioboard.Models.FacebookGroup();
                            objAddFacebookGroup.ProfileGroupId = item["id"].ToString();
                            objAddFacebookGroup.Name = item["name"].ToString();
                            objAddFacebookGroup.AccessToken = accessToken.ToString();
                            lstAddFacebookGroup.Add(objAddFacebookGroup);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.StackTrace);
                        }
                    }
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            return lstAddFacebookGroup;
        }

        public static Domain.Socioboard.Models.Facebookpage GetFbPageDetails(string url, Helper.AppSettings _appSettings)
        {
            FacebookClient fb = new FacebookClient();
            fb.AccessToken = _appSettings.AccessToken1;
            dynamic pageinfo = null;
            int fancountPage = 0;
            try
            {
                pageinfo = fb.Get(url);
            }
            catch(Exception ex)
            {
                try
                {
                    fb.AccessToken = _appSettings.AccessToken2;
                    pageinfo = fb.Get(url);
                }
                catch (Exception e)
                {
                    try
                    {
                        fb.AccessToken = _appSettings.AccessToken3;
                        pageinfo = fb.Get(url);
                    }
                    catch (Exception exe)
                    {
                        try
                        {
                            fb.AccessToken = _appSettings.AccessToken4;
                            pageinfo = fb.Get(url);
                        }
                        catch (Exception exx)
                        {

                        }
                    }
                }
            }
            Domain.Socioboard.Models.Facebookpage _facebookpage = new Facebookpage();
            _facebookpage.ProfilePageId = pageinfo["id"];
            try
            {
                _facebookpage.Name = pageinfo["username"];
            }
            catch (Exception)
            {
                _facebookpage.Name = pageinfo["name"];
            }
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls;
                dynamic friends = fb.Get("v2.7/" + _facebookpage.ProfilePageId);//v2.1
                fancountPage = Convert.ToInt32(friends["likes"].ToString());
            }
            catch 
            {
                fancountPage = 0;
            }
            _facebookpage.friendsCount = fancountPage;
            _facebookpage.AccessToken = fb.AccessToken;
            return _facebookpage;
        }

        public static string getFacebookRecentPost(string fbAccesstoken, string pageId)
        {
            string output = string.Empty;
            string facebookSearchUrl = "https://graph.facebook.com/v1.0/" + pageId + "/posts?limit=30&access_token=" + fbAccesstoken;
            var facebooklistpagerequest = (HttpWebRequest)WebRequest.Create(facebookSearchUrl);
            facebooklistpagerequest.Method = "GET";
            facebooklistpagerequest.Credentials = CredentialCache.DefaultCredentials;
            facebooklistpagerequest.AllowWriteStreamBuffering = true;
            facebooklistpagerequest.ServicePoint.Expect100Continue = false;
            facebooklistpagerequest.PreAuthenticate = false;
            try
            {
                using (var response = facebooklistpagerequest.GetResponse())
                {
                    using (var stream = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(1252)))
                    {
                        output = stream.ReadToEnd();
                    }
                }
            }
            catch (Exception e)
            {

            }
            return output;
        }

        public static void schedulePage_Post(string accessToken, string id,int TimeInterVal)
        {
            try
            {
                int i = 1;
                string curser_next = string.Empty;
                do
                {
                    string feeds = Fbpages.getFacebookPageRecentPost(accessToken, id, curser_next);
                    string feedId = string.Empty;
                   
                    if (!string.IsNullOrEmpty(feeds) && !feeds.Equals("[]"))
                    {
                        JObject fbpageNotes = JObject.Parse(feeds);
                        try
                        {
                            curser_next = fbpageNotes["paging"]["next"].ToString();
                        }
                        catch (Exception ex)
                        {
                            curser_next = "0";
                        }
                        foreach (JObject obj in JArray.Parse(fbpageNotes["data"].ToString()))
                        {
                            try
                            {
                                string feedid = obj["id"].ToString();
                                feedid = feedid.Split('_')[1];
                                double timestamp = Helper.DateExtension.ConvertToUnixTimestamp(DateTime.UtcNow.AddMinutes(TimeInterVal * i));
                                string link = "https://www.facebook.com/" + id + "/posts/" + feedid;
                                string ret = Fbpages.schedulePage_Post(accessToken, link, timestamp.ToString());
                                if (!string.IsNullOrEmpty(ret))
                                {
                                    i++;
                                }
                            }
                            catch { }

                        }
                    }
                } while (curser_next != "0");

            }
            catch (Exception ex)
            {
            }
        }

       
    }
}

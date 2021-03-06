﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Norma.Gamma;
using Norma.Gamma.Models;

namespace Norma.Eta.Models
{
    public class AbemaApiClient
    {
        private readonly AbemaTv _abemaTv;
        private readonly Configuration _configuration;

        public AbemaApiClient(Configuration configuration)
        {
            _configuration = configuration;
            _abemaTv = string.IsNullOrWhiteSpace(_configuration.Root.AccessToken)
                ? new AbemaTv()
                : new AbemaTv(_configuration.Root.AccessToken);
        }

        public void Initialize()
        {
            if (string.IsNullOrWhiteSpace(_abemaTv.AccessToken))
                GetToken();
            else
            {
                try
                {
                    var token = _abemaTv.Root.Token(osName => "pc", osVersion => "1.0.0",
                                                    osLang => "ja", osTimezone => "Asia/Tokyo",
                                                    appVersion => "v1.0.41");
                    Debug.WriteLine($"AccessToken is valid : {token.Token}");
                }
                catch
                {
                    _abemaTv.AccessToken = "";
                    GetToken();
                }
            }
        }

        private void GetToken()
        {
            var devId = Guid.NewGuid().ToString();
            var secret = _abemaTv.Preload.GetSecretKey(devId);
            var user = _abemaTv.Users.Verify(applicationKeySecret => secret, deviceId => devId);
            _abemaTv.AccessToken = user.Token;
            _configuration.Root.AccessToken = user.Token;
        }

        public async Task<Comments> Comments(string slotId)
        {
            try
            {
                return await _abemaTv.Root.CommentsAsync(slotId, limit => 20);
            }
            catch
            {
                return null;
            }
        }

        public async Task<Comment> Comment(string slotId, string comment)
        {
            try
            {
                return await _abemaTv.Root.CommentAsync(slotId, message => comment, share => null);
            }
            catch
            {
                return null;
            }
        }

        public Media MediaOfDays(int days)
        {
            var today = DateTime.Today.ToString("yyyyMMdd");
            var firstDayOfNextWeek = DateTime.Today.AddDays(days - 1).ToString("yyyyMMdd");
            return _abemaTv.Root.Media(dateFrom => today, dateTo => firstDayOfNextWeek);
        }

        public async Task<Media> MediaOfDaysAsync(int days)
        {
            var today = DateTime.Today.ToString("yyyyMMdd");
            var firstDayOfNextWeek = DateTime.Today.AddDays(days - 1).ToString("yyyyMMdd");
            return await _abemaTv.Root.MediaAsync(dateFrom => today, dateTo => firstDayOfNextWeek);
        }

        public async Task<Slot> CurrentSlot(string slotId)
            => await _abemaTv.Root.SlotDetailAsync(slotId);
    }
}
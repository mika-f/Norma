﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using Newtonsoft.Json;

using Norma.Eta.Models.Reservations;
using Norma.Gamma.Models;

namespace Norma.Eta.Models
{
    public class Reservation
    {
        private ObservableCollection<Reserve> ReservationsInternal { get; set; }

        public ReadOnlyObservableCollection<Reserve> Reservations { get; }

        public ReadOnlyCollection<RsvProgram> RsvsByProgram
            => ReservationsInternal.OfType<RsvProgram>().ToList().AsReadOnly();

        public ReadOnlyCollection<RsvTime> RsvsByTime
            => ReservationsInternal.OfType<RsvTime>().ToList().AsReadOnly();

        public ReadOnlyCollection<RsvKeyword> RsvsByKeyword
            => ReservationsInternal.OfType<RsvKeyword>().ToList().AsReadOnly();

        public Reservation()
        {
            Load();
            Save(false);
            Reservations = new ReadOnlyObservableCollection<Reserve>(ReservationsInternal);
        }

        private void Load()
        {
            if (!File.Exists(NormaConstants.ReserveProgramListFile))
            {
                ReservationsInternal = new ObservableCollection<Reserve>();
                Migrate();
                return;
            }
            using (var sr = File.OpenText(NormaConstants.ReserveProgramListFile))
            {
                var jsonSettings = new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.Auto};
                ReservationsInternal = JsonConvert.DeserializeObject<ObservableCollection<Reserve>>(sr.ReadToEnd(),
                                                                                                    jsonSettings);
            }
            Migrate();
        }

        public void Reload()
        {
            Load();
            Save(false);
        }

        public void Save(bool isLock = true)
        {
            using (var sw = File.CreateText(NormaConstants.ReserveProgramListFile))
            {
                var jsonSettings = new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.Auto};
                sw.WriteLine(JsonConvert.SerializeObject(ReservationsInternal.Where(w => w.IsEnable), jsonSettings));
            }
            if (!isLock)
                return;
            if (!File.Exists(NormaConstants.ReserveProgramLockFile))
                File.Create(NormaConstants.ReserveProgramLockFile);
        }

        private void Migrate()
        {
            // Not yet
        }

        /// <summary>
        ///     Slot を対象とした視聴予約を追加します。
        /// </summary>
        /// <param name="slot"></param>
        public void AddReservation(Slot slot)
        {
            ReservationsInternal.Add(new RsvProgram {ProgramId = slot.Id, StartDate = slot.StartAt});
            Save();
        }

        /// <summary>
        ///     時間を対象とした視聴予約を追加します。
        /// </summary>
        /// <param name="time"></param>
        /// <param name="repetition"></param>
        /// <param name="range"></param>
        public void AddReservation(DateTime time, RepetitionType repetition, DateRange range)
        {
            ReservationsInternal.Add(new RsvTime {StartTime = time, DayOfWeek = repetition, Range = range});
            Save();
        }

        /// <summary>
        ///     キーワード及び正規表現を対象とした視聴予約を追加します。
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="isRegex"></param>
        /// <param name="range"></param>
        public void AddReservation(string keyword, bool isRegex, DateRange range)
        {
            ReservationsInternal.Add(new RsvKeyword {Keyword = keyword, IsRegexMode = isRegex, Range = range});
            Save();
        }
    }
}
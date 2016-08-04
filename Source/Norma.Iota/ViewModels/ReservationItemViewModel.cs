﻿using Norma.Iota.Models;

namespace Norma.Iota.ViewModels
{
    internal class ReservationItemViewModel
    {
        private readonly ReservationItem _reservationItem;

        public string Type => _reservationItem.Type;
        public string Title => _reservationItem.Title ?? "-";
        public string StartAt => _reservationItem.StartAt?.ToString("g") ?? "-";

        public ReservationItemViewModel(ReservationItem reservationItem)
        {
            _reservationItem = reservationItem;
        }
    }
}
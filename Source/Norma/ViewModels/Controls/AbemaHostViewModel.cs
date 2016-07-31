﻿using System;
using System.Linq;
using System.Threading;

using CefSharp;
using CefSharp.Wpf;

using Newtonsoft.Json.Linq;

using Norma.Delta.Services;
using Norma.Eta.Models.Enums;
using Norma.Eta.Models.Operations;
using Norma.Eta.Mvvm;
using Norma.Models;

using Reactive.Bindings.Extensions;

namespace Norma.ViewModels.Controls
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class AbemaHostViewModel : ViewModel, IOperationRequestAware, INetworkCaptureRequestAware
    {
        private readonly AbemaState _abemaState;
        private readonly ReservationService _reservationService;
        private readonly TimetableService _timetableService;
        private JavaScriptHost _javaScritHost;

        public AbemaHostViewModel(AbemaState abemaState, Connector connector, NetworkHandler networkHandler,
                                  ReservationService reservationService, TimetableService timetableService)
        {
            _abemaState = abemaState;
            _reservationService = reservationService;
            _timetableService = timetableService;

            _abemaState.ObserveProperty(w => w.CurrentChannel).SubscribeOnUIDispatcher()
                       .Subscribe(w => Address = $"https://abema.tv/now-on-air/{w.Id}");

            connector.RegisterInsance<ChangeChannelOp>(this);
            networkHandler.RegisterInstance(this, e => e.Url.EndsWith("/slotReservations"));
            Address = "https://abema.tv/";
        }

        #region Implementation of INetworkCaptureRequestAware

        public void OnRequestHandling(NetworkEventArgs e)
        {
            dynamic json = JObject.Parse(e.Contents);
            var id = (string) json.slotReservations[0].slotId;
            _reservationService.InsertSlotReservation2(id);
        }

        #endregion

        #region Implementation of IOperationRequestAware

        public void Invoke(IOperation operation)
        {
            var args = operation as ChangeChannelOp;
            var channel = AbemaChannelExt.ToIdentifier(args?.Context.ToString());
            do
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
            }
            while (_javaScritHost == null);
            _abemaState.CurrentChannel = _timetableService.Channels.Single(w => w.Id == channel);
        }

        #endregion

        private void WebBrowserInitialized()
        {
            if (WebBrowser == null)
                return;
            _javaScritHost = new JavaScriptHost(WebBrowser);
        }

        #region Overrides of ViewModel

        public override void Dispose()
        {
            base.Dispose();

            WebBrowser.Dispose();
            Cef.Shutdown();
        }

        #endregion

        #region WebBrowser

        private IWpfWebBrowser _webBrowser;

        public IWpfWebBrowser WebBrowser
        {
            get { return _webBrowser; }
            set
            {
                if (SetProperty(ref _webBrowser, value))
                    WebBrowserInitialized();
            }
        }

        #endregion

        #region Address

        private string _address;

        public string Address
        {
            get { return _address; }
            set { SetProperty(ref _address, value); }
        }

        #endregion
    }
}
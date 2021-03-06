﻿using Norma.Eta.Mvvm;
using Norma.Models;

namespace Norma.ViewModels
{
    internal class StartupScreenViewModel : ViewModel
    {
        public string Name => ProductInfo.Name;

        public string Version => $"Version {ProductInfo.Version} {ProductInfo.ReleaseType.ToVersionString()}".Trim();

        public string Copyright => ProductInfo.Copyright;
    }
}
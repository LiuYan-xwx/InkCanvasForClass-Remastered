using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace InkCanvasForClass_Remastered.ViewModels
{
    public partial class TimeViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _nowTime;

        [ObservableProperty]
        private string _nowDate;

    }
}

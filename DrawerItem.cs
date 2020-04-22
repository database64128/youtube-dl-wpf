using System;
using System.Collections.Generic;
using System.Text;

namespace youtube_dl_wpf
{
    public class DrawerItem : ViewModelBase
    {
        private string _name;
        private object _content;

        public DrawerItem(string name, object content)
        {
            _name = name;
            _content = content;
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public object Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }
    }
}

using ReactiveUI;

namespace YoutubeDl.Wpf.ViewModels
{
    public class DrawerItem : ReactiveObject
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
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public object Content
        {
            get => _content;
            set => this.RaiseAndSetIfChanged(ref _content, value);
        }
    }
}

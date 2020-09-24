namespace YoutubeDl.Wpf.ViewModels
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

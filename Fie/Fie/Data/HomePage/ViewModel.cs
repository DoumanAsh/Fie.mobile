using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Xamarin.Forms;

namespace Fie.Data.HomePage {
    public class Tag : INotifyPropertyChanged {
        public int id;
        public string _text;
        public string text {
            set {
                _text = value;
                on_property_change();
            }
            get => _text;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void on_property_change([CallerMemberName] string name = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public override string ToString() {
            return _text;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public override bool Equals(object obj) {
            return !(obj is Tag right) ? false : this.id == right.id;
        }
    }

    public class ViewModel : INotifyPropertyChanged {
        //Utility to mark with uniquness tags
        public int current_idx = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void on_property_change([CallerMemberName] string name = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public Command post_tweet { private set; get; }
        public Command open_file { private set; get; }
        public Command new_tag { private set; get; }
        public Command<Tag> delete_tag { private set; get; }

        public int list_tags_size = 0;
        public ObservableCollection<Tag> list_tags { get; set; } = new ObservableCollection<Tag> { };

        //Post's text
        private string _text;

        public string text {
            set {
                _text = value;
                on_property_change();
                post_tweet.ChangeCanExecute();
            }
            get => _text;
        }

        //Tag's text
        private string _tag;

        public string tag {
            set {
                _tag = value;
                on_property_change();
            }
            get => _tag;
        }

        private void after_post() {
            text = null;
            //TODO: give option to not clear tags?
            list_tags.Clear();
            list_tags_size = 0;
        }

        private string get_post_text() {
            if (list_tags_size == 0) return text;

            var buffer = new StringBuilder(text, text.Length + list_tags_size * 10 + 1);
            buffer.Append('\n');

            foreach (var tag in list_tags) {
                string tag_text = tag.ToString().Trim();
                if (tag_text.Length != 0) {
                    buffer.AppendFormat("#{0} ", tag.ToString().Trim());
                }
            }
            
            buffer.Length -= 1;
            return buffer.ToString();
        }

        public ViewModel() {
            post_tweet = new Command(
                execute: async () => {
                    string post_text = get_post_text();

                    try {
                        var task = API.Twitter.post_tweet(post_text, new API.Options());
                        after_post();
                        await task;
                    } catch (Exception error) {
#if DEBUG
                        Console.WriteLine("Fie: error: {0}", error);
#endif
                        MessagingCenter.Send(this, Misc.DisplayAlert.NAME, new Misc.DisplayAlert {
                            title = "Failed to post",
                            message = "Error posting on twitter",
                            accept = "Ok",
                        });
                    }
                },
                canExecute: () => {
                    return text != null && text.Length > 0;
                }
            );
            open_file = new Command(
                execute: () => {
                    Console.WriteLine("Fie: pick file");
                },
                canExecute: () => {
                    return true;
                }
            );
            new_tag = new Command(
                execute: () => {
                    list_tags.Add(new Tag {
                        text = "",
                        id = current_idx
                    });
                    current_idx += 1;
                    list_tags_size += 1;
                }
            );
            delete_tag = new Command<Tag>(
                execute: (self) => {
                    list_tags.Remove(self);
                    list_tags_size -= 1;
                }
            );
        }
    }
}
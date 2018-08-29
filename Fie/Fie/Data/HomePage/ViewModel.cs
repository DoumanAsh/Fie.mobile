using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Config;
using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;
using Xamarin.Forms;

namespace Fie.Data.HomePage {
    public class Image {
        public int id;
        public string mime;
        public FileData data;

        public string file_name {
            get => data.FileName;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public override bool Equals(object obj) {
            return !(obj is Image right) ? false : this.id == right.id;
        }
    }

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
        private ApiConfig config;
        //Utility to mark with uniquness tags
        public int current_idx = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void on_property_change([CallerMemberName] string name = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public Command post { private set; get; }
        public Command open_file { private set; get; }
        public Command new_tag { private set; get; }
        public Command<Tag> delete_tag { private set; get; }
        public Command<Image> delete_image { private set; get; }

        public int list_tags_size = 0;
        public ObservableCollection<Tag> list_tags { get; set; } = new ObservableCollection<Tag> { };
        private int IMAGES_MAX_SIZE = 4;
        public int list_images_size = 0;
        public ObservableCollection<Image> list_images { get; set; } = new ObservableCollection<Image> { };

        //Post's text
        private string _text;

        public string text {
            set {
                _text = value;
                on_property_change();
                post.ChangeCanExecute();
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

        private async Task pick_file() {
            FileData file_data = await CrossFilePicker.Current.PickFile();
            if (file_data == null) {
#if DEBUG
                Console.WriteLine("Fie: No file is choose");
#endif
                return; // user canceled file picking
            }

            var mime = API.API.guess_image_mime(file_data.FileName);
#if DEBUG
            Console.WriteLine("Fie: Image name chosen: {0}", file_data.FilePath);
#endif
            if (mime == null) {
                //unsupported image type
#if DEBUG
                Console.WriteLine("Fie: Invalid image type");
#endif
                file_data.Dispose();
                return;
            }

            list_images.Add(new Image {
                mime = mime,
                data = file_data,
                id = current_idx,
            });
            list_images_size += 1;
            current_idx += 1;
            open_file.ChangeCanExecute();

            return;
        }

        private async Task<bool> post_tweet(string text) {
            try {
                await API.Twitter.post_tweet(text, new API.Options());

                return true;
            } catch (Exception error) {
#if DEBUG
                Console.WriteLine("Fie: error: {0}", error);
#endif
                MessagingCenter.Send(this, Misc.DisplayAlert.NAME, new Misc.DisplayAlert {
                    title = "Failed to post",
                    message = "Error posting on twitter",
                    accept = "Ok",
                });

                return false;
            }
        }

        public ViewModel() {
            config = ((App)Application.Current).config();

            post = new Command(
                execute: async () => {
                    string post_text = get_post_text();
                    bool finished = false;

                    if (this.config.twitter.enabled) {
                        finished = await this.post_tweet(post_text);
                    }

                    if (finished) {
                        after_post();
                    }
                },
                canExecute: () => {
                    return text != null && text.Length > 0;
                }
            );
            open_file = new Command(
                execute: async () => {
                    await this.pick_file();
                },
                canExecute: () => {
                    return list_images_size < IMAGES_MAX_SIZE;
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
            delete_image = new Command<Image>(
                execute: (self) => {
                    list_images.Remove(self);
                    list_images_size -= 1;
                    open_file.ChangeCanExecute();
                }
            );
        }
    }
}
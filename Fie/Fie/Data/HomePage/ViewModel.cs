using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;
using Xamarin.Forms;

using Config;
using Logging;
using System.Net.Http;

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
        private bool is_ongoing = false;
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
        public ObservableCollection<Tag> list_tags { get; set; } = new ObservableCollection<Tag>();
        private int IMAGES_MAX_SIZE = 4;
        public int list_images_size = 0;
        public ObservableCollection<Image> list_images { get; set; } = new ObservableCollection<Image>();

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

        //Settings
        public bool _nsfw = false;
        public bool nsfw {
            set {
                _nsfw = value;
                on_property_change();
            }
            get => _nsfw;
        }

        public bool _clear_tags = true;
        public bool clear_tags {
            set {
                _clear_tags = value;
                on_property_change();
            }
            get => _clear_tags;
        }

        //Platforms
        public bool is_on_twitter {
            set {
                this.config.twitter.enabled = value;
                on_property_change();
            }
            get => this.config.twitter.enabled;
        }
        public bool is_on_gab {
            set {
                this.config.gab.enabled = value;
                on_property_change();
            }
            get => this.config.gab.enabled;
        }
        public bool is_on_minds {
            set {
                this.config.minds.enabled = value;
                on_property_change();
            }
            get => this.config.minds.enabled;
        }
        public bool is_on_mastodon {
            set {
                this.config.mastodon.enabled = value;
                on_property_change();
            }
            get => this.config.mastodon.enabled;
        }
        

        private void after_post() {
            text = null;
            nsfw = false;

            if (clear_tags) {
                list_tags.Clear();
                list_tags_size = 0;
            }

            list_images.Clear();
            list_images_size = 0;
            open_file.ChangeCanExecute();
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
                Debug.log("Fie: No file is choose");
                return; // user canceled file picking
            }

            var mime = API.API.guess_image_mime(file_data.FileName);
            Debug.log("Fie: Image name chosen: {0}", file_data.FilePath);

            if (mime == null) {
                //unsupported image type
                Debug.log("Fie: Invalid image type");
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

        private async Task<bool> post_tweet(string text, API.Options opts) {
            try {
                var tweet = await API.Twitter.post_tweet(text, opts);

                if (tweet != null) {
                    return true;
                }
            } catch (Exception error) {
                Debug.log("Fie: Tweet error: {0}", error);
            }

            MessagingCenter.Send(this, Misc.DisplayAlert.NAME, new Misc.DisplayAlert {
                title = "Failed to post",
                message = "Error posting on twitter",
                accept = "Ok",
            });

            return false;
        }

        void message_network_error() {
            MessagingCenter.Send(this, Misc.DisplayAlert.NAME, new Misc.DisplayAlert {
                title = "Network Error",
                message = "Unable to connect. Please turn on network",
                accept = "Ok",
            });
        }

        private async Task<bool> post_gab(string text, API.Options opts) {
            try {
                await API.Gab.login(this.config.gab.username, this.config.gab.passowrd);
                if (await API.Gab.post(text, opts)) {
                    return true;
                }
            } catch (HttpRequestException error) {
                Debug.log("Fie: Http error: {0}", error);
                message_network_error();
                return false;
            } catch (Exception error) {
                Debug.log("Fie: Gab error: {0}", error);
            }
 
            MessagingCenter.Send(this, Misc.DisplayAlert.NAME, new Misc.DisplayAlert {
                title = "Failed to post",
                message = "Error posting on gab",
                accept = "Ok",
            });

            return false;
        }
        private async Task<bool> post_mastodon(string text, API.Options opts) {
            try {
                if (API.Mastodon.configure(this.config.mastodon.host, this.config.mastodon.access_token)) {
                    if (await API.Mastodon.post(text, opts)) {
                        return true;
                    }
                }
            } catch (HttpRequestException error) {
                Debug.log("Fie: Http error: {0}", error);
                message_network_error();
                return false;
            } catch (Exception error) {
                Debug.log("Fie: Mastodon error: {0}", error);
            }
 
            MessagingCenter.Send(this, Misc.DisplayAlert.NAME, new Misc.DisplayAlert {
                title = "Failed to post",
                message = "Error posting on Mastodon",
                accept = "Ok",
            });

            return false;
        }

        private async Task<bool> post_minds(string text, API.Options opts) {
            try {
                await API.Minds.login(this.config.minds.username, this.config.minds.passowrd);
                if (await API.Minds.post(text, opts)) {
                    return true;
                }
            } catch (HttpRequestException error) {
                Debug.log("Fie: Http error: {0}", error);
                message_network_error();
                return false;
            } catch (Exception error) {
                Debug.log("Fie: Minds error: {0}", error);
            }
 
            MessagingCenter.Send(this, Misc.DisplayAlert.NAME, new Misc.DisplayAlert {
                title = "Failed to post",
                message = "Error posting on minds",
                accept = "Ok",
            });

            return false;
        }


        public ViewModel() {
            config = ((App)Application.Current).config();

            post = new Command(
                execute: async () => {
                    if (is_ongoing) return;
                    else if (!is_on_twitter && !is_on_gab && !is_on_minds && !is_on_mastodon) {
                        Debug.log("Fie: nothing to do, ignore post");
                        return;
                    };

                    is_ongoing = true;
                    
                    Debug.log("Fie: post");
                    string post_text = get_post_text();

                    if (this.is_on_twitter && !API.Twitter.can_post(post_text)) {
                        Debug.log("Fie: Tweet text is too long. Abort");
                        MessagingCenter.Send(this, Misc.DisplayAlert.NAME, new Misc.DisplayAlert {
                            title = "Failure",
                            message = "Tweet size is too big. Trim it down before trying again",
                            accept = "Ok",
                        });
                    }

                    Debug.log("Fie: Post text={0} | NSFW={1}", post_text, nsfw);
                    bool finished = true;

                    var opts = new API.Options {
                        nsfw = this.nsfw
                    };

                    foreach (var img in list_images) {
                        Debug.log("Fie: add_image");
                        opts.add_image(img.mime, img.file_name, img.data.DataArray);
                    }

                    if (this.is_on_twitter) {
                        Debug.log("Fie: twitter post");
                        var result = await this.post_tweet(post_text, opts);
                        finished = finished && result;
                    }
                    if (this.is_on_gab) {
                        Debug.log("Fie: gab post");
                        var result = await this.post_gab(post_text, opts);
                        finished = finished && result;
                    }
                    if (this.is_on_mastodon) {
                        Debug.log("Fie: mastodon post");
                        var result = await this.post_mastodon(post_text, opts);
                        finished = finished && result;
                    }
                    if (this.is_on_minds) {
                        Debug.log("Fie: minds post");
                        var result = await this.post_minds(post_text, opts);
                        finished = finished && result;
                    }

                    if (finished) {
                        Debug.log("Fie: finished posting");
                        after_post();
                        MessagingCenter.Send(this, Misc.DisplayAlert.NAME, new Misc.DisplayAlert {
                            title = "Success",
                            message = "Post has been shared on all social medias",
                            accept = "Ok",
                        });
                    } else {
                        Debug.log("Fie: not completely done");
                    }

                    is_ongoing = false;
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
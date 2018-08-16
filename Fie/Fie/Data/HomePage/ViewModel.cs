﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Xamarin.Forms;

namespace Fie.Data.HomePage {
    public class Tag : INotifyPropertyChanged {
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
    }

    public class ViewModel : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void on_property_change([CallerMemberName] string name = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public Command post_tweet { private set; get; }
        public Command open_file { private set; get; }
        public Command new_tag { private set; get; }
        public Command<int> delete_tag { private set; get; }

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

        public ViewModel() {
            post_tweet = new Command(
                execute: async () => {
                    try {
                        var task = API.Twitter.post_tweet(text, new API.Options());
                        text = null;
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
                        text = "New tag"
                    });
                }
            );
            delete_tag = new Command<int>(
                execute: (idx) => {
                    list_tags.RemoveAt(idx);
                }
            );
        }
    }
}
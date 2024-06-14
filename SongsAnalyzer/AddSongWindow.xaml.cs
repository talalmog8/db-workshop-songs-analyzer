using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualBasic;
using Model;
using Model.Contract;
using Model.Entities;

namespace SongsAnalyzer
{
    public partial class AddSongWindow : Window
    {
        private const string SPACE = " ";

        private void AddComposerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_songAnalyzer.Processed)
            {
                SongIsNotProcessed();
                return;
            }
            
            var composer = Interaction.InputBox("Enter composer name:", "Add Composer", "");
            if (!string.IsNullOrEmpty(composer) && composer.Split(SPACE, StringSplitOptions.RemoveEmptyEntries).Length > 1) 
                ComposersListBox.Items.Add(composer);
            else 
                FullNameError();
        }

        private void AddWriterButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_songAnalyzer.Processed)
            {
                SongIsNotProcessed();
                return;
            }
            
            var writer = Interaction.InputBox("Enter writer name:", "Add Writer", "");
            if (!string.IsNullOrEmpty(writer) && writer.Split(SPACE, StringSplitOptions.RemoveEmptyEntries).Length > 1)
                WritersListBox.Items.Add(writer);
            else 
                FullNameError();
        }
        
        private void AddPerformerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_songAnalyzer.Processed)
            {
                SongIsNotProcessed();
                return;
            }
            
            var performer = Interaction.InputBox("Enter performer full name. use a space as a separator:", "Add Performer", "");
            if (!string.IsNullOrEmpty(performer) && performer.Split(SPACE, StringSplitOptions.RemoveEmptyEntries).Length > 1)
                PerformersListBox.Items.Add(performer);
            else 
                FullNameError();
        }

        private async void AddSongButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_songAnalyzer.Processed)
            {
                SongIsNotProcessed();
                return;
            }
            
            var composers = new HashSet<Name>();
            var writers = new HashSet<Name>();
            var performers = new HashSet<Name>();

            PopulateSet(composers, ComposersListBox.Items);
            PopulateSet(writers, WritersListBox.Items);
            PopulateSet(performers, PerformersListBox.Items);

            try
            {
                await _songAnalyzer.AddSong(composers, performers, writers);
                ComposersListBox.Dispatcher.InvokeAsync(() => ComposersListBox.Items.Clear());
                WritersListBox.Dispatcher.InvokeAsync(() => WritersListBox.Items.Clear());
                PerformersListBox.Dispatcher.InvokeAsync(() => PerformersListBox.Items.Clear());
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Failed to add song. {exception}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateSet(HashSet<Name> hashSet, ItemCollection collection)
        {
            foreach (var item in collection)
            {
                if(item is not null && !string.IsNullOrEmpty(item?.ToString()))
                {
                    var fullName = item!.ToString();
                    var sep = fullName!.IndexOf(SPACE, StringComparison.Ordinal);
                    var firstName = fullName[..sep].Trim();
                    var lastName = fullName[(sep + 1)..].Trim();
                    hashSet.Add(new Name(firstName, lastName));
                }
            }
        }

        private static void FullNameError()
        {
            MessageBox.Show("Please use a full name, use a space as a separator", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }



    }
}

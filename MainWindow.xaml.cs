using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;

namespace Dalister
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Point _dragStartPoint;
        private bool _isDragging = false;

        public MainWindow()
        {
            InitializeComponent();
        }


        private void button_Click(object sender, RoutedEventArgs e)
        {
            // Adds the string in the textbox to the listbox
            string text = textBox.Text.Trim();

            if (!string.IsNullOrEmpty(text))
            {
                listBox.Items.Add(text);
                textBox.Clear();
            }
            else
            {
                MessageBox.Show("There is nothing in the field. Please enter your task", "Whoops!", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
        // Helper method to get ListBoxItem under mouse position
        private ListBoxItem GetListBoxItemUnderMouse(Point position)
        {
            var element = listBox.InputHitTest(position) as DependencyObject;

            while (element != null && !(element is ListBoxItem))
            {
                element = VisualTreeHelper.GetParent(element);
            }

            return element as ListBoxItem;
        }

        private void listBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);

            if (e.ClickCount == 1 && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
            {
                Point mousePos = e.GetPosition(listBox);
                var item = GetListBoxItemUnderMouse(mousePos);

                if (item != null)
                {
                    string task = item.Content.ToString();
                    int index = listBox.ItemContainerGenerator.IndexFromContainer(item);

                    if (!task.StartsWith("✔ "))
                        listBox.Items[index] = "✔ " + task;
                    else
                        listBox.Items[index] = task.Substring(2);
                }
            }
        }

        private void listBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                Point currentPosition = e.GetPosition(null);

                if ((SystemParameters.MinimumHorizontalDragDistance < Math.Abs(currentPosition.X - _dragStartPoint.X)) ||
                    (SystemParameters.MinimumVerticalDragDistance < Math.Abs(currentPosition.Y - _dragStartPoint.Y)))
                {
                    Point mousePos = e.GetPosition(listBox);
                    var item = GetListBoxItemUnderMouse(mousePos);

                    if (item == null)
                        return;

                    _isDragging = true;

                    string draggedItem = item.Content.ToString();
                    DataObject data = new DataObject(typeof(string), draggedItem);
                    DragDrop.DoDragDrop(listBox, data, DragDropEffects.Move);

                    _isDragging = false;
                }
            }
        }

        private void listBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition(listBox);
            var item = GetListBoxItemUnderMouse(mousePos);

            if (item != null)
            {
                string task = item.Content.ToString();
                int index = listBox.ItemContainerGenerator.IndexFromContainer(item);

                MessageBoxResult result = MessageBox.Show(
                    $"Delete task: \"{task}\"?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                    listBox.Items.RemoveAt(index);
            }
        }


        // Ctrl+P marks priority
        private void listBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.P)
            {
                e.Handled = true;
                MarkAsPriority();
            }
        }

        private void MarkAsPriority()
        {
            if (listBox.SelectedItem != null)
            {
                string task = listBox.SelectedItem.ToString();
                if (!task.StartsWith("⭐ "))
                    listBox.Items[listBox.SelectedIndex] = "⭐ " + task;
                else
                    MessageBox.Show("This task is already marked as priority.");
            }
        }

        // Handle drop to reorder
        private void listBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(string)))
            {
                string droppedData = e.Data.GetData(typeof(string)) as string;
                int index = GetCurrentIndex(e.GetPosition);

                if (index < 0) index = listBox.Items.Count - 1;

                listBox.Items.Remove(droppedData);
                listBox.Items.Insert(index, droppedData);
            }
        }

        private int GetCurrentIndex(GetPositionDelegate getPosition)
        {
            for (int i = 0; i < listBox.Items.Count; i++)
            {
                ListBoxItem item = (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromIndex(i);
                if (item != null && IsMouseOverTarget(item, getPosition))
                    return i;
            }
            return -1;
        }

        private bool IsMouseOverTarget(Visual target, GetPositionDelegate getPosition)
        {
            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            Point mousePos = getPosition((IInputElement)target);
            return bounds.Contains(mousePos);
        }

        private delegate Point GetPositionDelegate(IInputElement element);

        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Dalister List Files (*.dalist)|*.dalist|All Files (*.*)|*.*";
            saveFileDialog.Title = "Save List to File";

            if (saveFileDialog.ShowDialog() == true)
            {
                using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName))
                {
                    foreach (var item in listBox.Items)
                    {
                        writer.WriteLine(item.ToString());
                    }
                }
                MessageBox.Show("Yay! Tasks saved successfully!", "Saved Succesfully", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void openBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Dalister List Files (*.dalist)|*.dalist|All Files (*.*)|*.*";
            openFileDialog.Title = "Open List from File";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string[] lines = File.ReadAllLines(openFileDialog.FileName);
                    listBox.Items.Clear();
                    foreach (string line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                            listBox.Items.Add(line);
                    }
                }
                catch (IOException ex)
                {
                    MessageBox.Show("Oh No! There was an error reading the file: " + ex.Message, "An Error Occured", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void about_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Dalister is an efficient, minimalist and non-distracting list tracker to keep track of what you were doing. \n" + "Copyright © Ligma Techlologies 2025, All Rights Reserved \n" + "Version 1.0.0", "About Dalister", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void new_Click(object sender, RoutedEventArgs e)
        {
            listBox.Items.Clear();
        }
    }


}
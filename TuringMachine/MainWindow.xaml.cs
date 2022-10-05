using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace TuringMachine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        public ObservableCollection<List<Cell>> ColumnsCollection { get; set; }

        public const int tapeCapacity = 15;

        public static int StatesCount = 0;

        public ObservableCollection<Rule> Rules { get; set; }

        public CellsTape Tape { get; set; }

        private bool isRunning = false;

        #endregion

        #region Internal classes

        public class CellsTape
        {
            public int start;
            public int finish;
            public List<string> tape;
            public int currentState = 1;

            public List<string> VisibleTapeFragment { get; set; }

            public CellsTape(List<string> visibleTapeFragment)
            {
                this.start = 0;
                this.finish = tapeCapacity;
                this.VisibleTapeFragment = new List<string>(visibleTapeFragment);
                this.tape = new List<string>(visibleTapeFragment);
            }

            public string this[int i]
            {
                get => this.tape[i];
                set => this.tape[i] = value;
            }
        }

        public class Cell
        {
            public string Value { get; set; }
            public Cell() => Value = string.Empty;
            public Cell(string val) => Value = val;
        }

        public class Rule
        {
            public string Symbol { get; set; }
            public List<Move> Moves { get; set; }

            public ObservableCollection<string> RawMoves { get; set; }

            /// <summary>
            /// Set symbol and init capacity moves
            /// </summary>
            /// <param name="symbol"></param>
            /// <param name="capacity"></param>
            public Rule(string symbol, int capacity)
            {
                this.Symbol = symbol;
                this.Moves = Enumerable.Range(0, capacity).Select(r => new Move(r + 1, symbol)).ToList();
                this.RawMoves = new ObservableCollection<string>(this.Moves.Select(m => m.ToString()));
            }
        }

        public class Move
        {
            public string RawValue { get; set; }
            public string NewValue { get; set; }
            public string Symbol { get; set; }
            public MoveDirection Direction { get; set; }
            public int NewState { get; set; }
            public int CurrentState { get; set; }

            /// <summary>
            /// Set state, symbol and try set value
            /// </summary>
            /// <param name="currentState"></param>
            /// <param name="Symbol"></param>
            /// <param name="rawValue"></param>
            public Move(int currentState, string Symbol, string rawValue)
            {
                this.Symbol = Symbol;
                this.CurrentState = currentState;
                this.NewValue = Symbol;
                this.TrySetRawValue(rawValue);
            }

            /// <summary>
            /// Set state, symbol. Value = string.Empty
            /// </summary>
            /// <param name="currentState"></param>
            /// <param name="Symbol"></param>
            public Move(int currentState, string Symbol)
            {
                this.CurrentState = currentState;
                this.Symbol = Symbol;
                this.Direction = MoveDirection.None;
                this.NewValue = string.Empty;
                this.NewState = currentState;
                this.NewValue = Symbol;
                this.RawValue = string.Empty;
            }

            public bool TrySetRawValue(string rawValue)
            {
                this.RawValue = rawValue;
                if (!this.ParseOnDirection(MoveDirection.Left) && !this.ParseOnDirection(MoveDirection.Right) && !this.ParseOnDirection(MoveDirection.None))
                {
                    this.RawValue = string.Empty;
                    return false;
                }
                return true;
            }

            public bool ParseOnDirection(MoveDirection direction)
            {
                var directionChar = GetEnumChar(direction);
                var data = (" " + this.RawValue).Split(directionChar);
                if (data.Length == 2 && int.TryParse(data[1], out var newState) && newState <= StatesCount)
                {
                    this.Direction = direction;
                    this.NewState = newState;
                    this.NewValue = data[0];
                    return true;
                }
                return false;
            }

            public override string ToString() => $" {this.NewValue} {GetEnumChar(this.Direction)} {this.NewState} ";
        }

        public enum MoveDirection
        {
            Left,
            Right,
            None
        };

        #endregion

        public static char GetEnumChar(MoveDirection changePosition)
        {
            switch (changePosition)
            {
                case MoveDirection.Left:
                    return '<';
                case MoveDirection.Right:
                    return '>';
                case MoveDirection.None:
                    return '.';
                default:
                    return default;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            Rules = new ObservableCollection<Rule>();
            Tape = new CellsTape(new List<string>(Enumerable.Repeat(string.Empty, tapeCapacity)));
            ColumnsCollection = new ObservableCollection<List<Cell>>(Enumerable.Repeat(new List<Cell>() { new Cell(string.Empty) }, tapeCapacity));

           VisibleTapeGrid.ItemsSource = ColumnsCollection;
            foreach (DataGridTextColumn c in VisibleTapeGrid.Columns)
            {
                c.IsReadOnly = false;
                c.Binding = new Binding("Value") { Mode = BindingMode.TwoWay };
            }
            RulesGrid.ItemsSource = Rules;
            AddStateButton_Click(null, null);
            AddRule(string.Empty);
        }

        public void ToLeftButton_Click(object sender, RoutedEventArgs e) => MoveLeft();

        private void MoveLeft()
        {
            var value = string.Empty;
            if (Tape.start != 0)
            {
                value = Tape.tape[Tape.start - 1];
                Tape.start--;
                Tape.finish--;
            }
            else
            {
                Tape.tape.Insert(0, value);
            }
            
            Tape.VisibleTapeFragment.Insert(0, value);
            Tape.VisibleTapeFragment.RemoveAt(tapeCapacity);
            ColumnsCollection.Insert(0, new List<Cell>() { new Cell(value) });
            ColumnsCollection.RemoveAt(tapeCapacity);
            VisibleTapeGrid.ItemsSource = ColumnsCollection;
        }

        public void ToRightButton_Click(object sender, RoutedEventArgs e) => MoveRight();

        private void MoveRight()
        {
            var value = string.Empty;
            if (Tape.finish != Tape.tape.Count)
            {
                value = Tape.tape[Tape.finish];
            }
            else
            {
                Tape.tape.Add(value);
            }

            Tape.tape.Add(value);
            Tape.VisibleTapeFragment.Add(value);
            Tape.VisibleTapeFragment.RemoveAt(0);
            Tape.start++;
            Tape.finish++;

            ColumnsCollection.Add(new List<Cell>() { new Cell(value) });
            ColumnsCollection.RemoveAt(0);
            VisibleTapeGrid.ItemsSource = ColumnsCollection;
        }

        public void RulesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if ((e?.EditingElement) is TextBox tb)
            {
                var x = e.Column.DisplayIndex;
                var y = e.Row.GetIndex();
                var newText = tb.Text;

                var currentMove = Rules[y].Moves[x];
                if (!currentMove.TrySetRawValue(newText))
                {
                    MessageBox.Show("Wrong value");
                    e.Cancel = true;
                }
                Rules[y].RawMoves[x] = currentMove.ToString();
            }
        }

        public void AddRuleButton_Click(object sender, RoutedEventArgs e)
        {
            var promptDialog = new PromptDialog();
            if (promptDialog.ShowDialog() != true
                || string.IsNullOrWhiteSpace(promptDialog.ResponseText)
                || Rules.FirstOrDefault(r => r.Symbol == promptDialog.ResponseText) != null)
            {
                return;
            }
            AddRule(promptDialog.ResponseText);
        }

        public void AddStateButton_Click(object sender, RoutedEventArgs e)
        {
            var column = new DataGridTextColumn();
            column.Header = $"Q{StatesCount + 1}";
            column.IsReadOnly = false;
            column.Binding = new Binding(string.Format("RawMoves[{0}]", StatesCount));
            foreach (var rule in Rules)
            {
                var newMove = new Move(StatesCount + 1, rule.Symbol);
                rule.Moves.Add(newMove);
                rule.RawMoves.Add(newMove.ToString());
            }
            RulesGrid.Columns.Add(column);
            StatesCount++;
        }

        private void RemoveStateButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedCells = RulesGrid.SelectedCells;
            if (selectedCells.Any() && Rules.Count > 1)
            {
                var index = selectedCells.First().Column.DisplayIndex;
                foreach (var rule in Rules)
                {
                    if (rule.Moves.Count < index)
                    {
                        return;
                    }
                    rule.Moves.RemoveAt(index);
                    rule.RawMoves.RemoveAt(index);
                }
                RulesGrid.Columns.RemoveAt(index);
                StatesCount--;

                foreach (DataGridTextColumn column in RulesGrid.Columns)
                {
                    column.Header = $"Q{column.DisplayIndex + 1}";
                    column.Binding = new Binding(string.Format("RawMoves[{0}]", column.DisplayIndex));
                }
            }
        }

        private void RemoveRuleButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRule = RulesGrid.SelectedIndex;
            if (selectedRule > -1 && Rules[selectedRule].Symbol != string.Empty)
            {
                Rules.RemoveAt(selectedRule);
            }
        }

        public void AddRule(string value) => Rules.Add(new Rule(value, StatesCount));

        public void StartButton_Click(object sender, RoutedEventArgs e) => new Thread(Play).Start();

        private void Play()
        {
            Tape.currentState = 1;
            this.Dispatcher.Invoke(() => DebugButton.IsEnabled = false);
            isRunning = true;
            this.Dispatcher.Invoke(() => ChangeEnability());
            while (Tape.currentState != 0)
            {
                this.Dispatcher.Invoke(() => MoveNext());
                if (isRunning && Tape.currentState != 0)
                {
                    Thread.Sleep(400);
                }
                else
                {
                    break;
                }
            }
            isRunning = false;
            MessageBox.Show("Programm executed");
            this.Dispatcher.Invoke(() => DebugButton.IsEnabled = true);
            this.Dispatcher.Invoke(() => ChangeEnability());
        }

        private void MoveNext()
        {
            var currentSymbol = Tape[Tape.start];
            var currentRule = Rules.FirstOrDefault(r => r.Symbol == currentSymbol) ?? Rules.FirstOrDefault(r => r.Symbol == string.Empty);
            if (currentRule == null)
            {
                Tape.currentState = 0;
                ChangeEnability();
                return;
            }
            var currentMove = currentRule.Moves[Tape.currentState - 1];
            Tape[Tape.start] = currentMove.NewValue;
            Tape.currentState = currentMove.NewState;

            switch (currentMove.Direction)
            {
                case MoveDirection.Left:
                    MoveLeft();
                    break;
                case MoveDirection.Right:
                    MoveRight();
                    break;
                case MoveDirection.None:
                    break;
                default:
                    break;
            }
        }

        private void ChangeEnability()
        {
            StartButton.IsEnabled = !isRunning;
            DebugButton.IsEnabled = !isRunning;
            RulesGrid.IsReadOnly = isRunning;
            VisibleTapeGrid.IsReadOnly = isRunning;
            AddRuleButton.IsEnabled = !isRunning;
            AddStateButton.IsEnabled = !isRunning;
            RemoveRuleButton.IsEnabled = !isRunning;
            RemoveStateButton.IsEnabled = !isRunning;
            ToLeftButton.IsEnabled = !isRunning;
            ToRightButton.IsEnabled = !isRunning;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            isRunning = false;
            ChangeEnability();
        }

        private void DebugButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isRunning)
            {
                isRunning = true;
                ChangeEnability();
                Tape.currentState = 1;
            }
            else if (Tape.currentState == 0)
            {
                MessageBox.Show("Program executed");
                return;
            }
            MoveNext();
        }

        private void VisibleTapeGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            e.Cancel = true;
            int index = VisibleTapeGrid.ItemContainerGenerator.IndexFromContainer(e.Row);
            if (index > -1 && index < tapeCapacity && e.EditingElement is TextBox tb)
            {
                Tape.VisibleTapeFragment[index] = tb.Text;
                ColumnsCollection[index][0] = new Cell(Tape.VisibleTapeFragment[index]);
                Tape[Tape.start + index] = Tape.VisibleTapeFragment[index];
            }
        }
    }
}

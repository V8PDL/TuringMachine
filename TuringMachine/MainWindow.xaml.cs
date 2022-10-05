using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        public const int capacity = 15;

        public static int movesCount = 0;

        public static int statesCount = 0;

        ObservableCollection<List<Move>> Moves { get; set; } = new ObservableCollection<List<Move>>();

        public ObservableCollection<Cell> VisibleTapeFragment { get; set; }

        public ObservableCollection<Rule> Rules { get; set; }
        public static ObservableCollection<string> VisibleTape { get => visibleTape; set => visibleTape = value; }
        public CellsTape Tape { get => tape; set => tape = value; }

        private static ObservableCollection<string> visibleTape;

        private CellsTape tape;

        #endregion

        #region Internal classes

        public class CellsTape
        {
            public ObservableCollection<Cell> visibleTapeFragment;
            public int start;
            public int finish;
            public List<Cell> tape;

            public CellsTape(ObservableCollection<Cell> visibleTapeFragment)
            {
                this.start = 0;
                this.finish = capacity;
                this.visibleTapeFragment = visibleTapeFragment;
                this.tape = new List<Cell>(this.visibleTapeFragment);
            }
        }

        public class Cell
        {
            public string value;
            public string Value
            {
                get { return this.value; }
                set { this.value = value; }
            }
            public Cell() => Value = "";
            public Cell(string val) => Value = val;
        }

        public class Rule
        {
            public string Symbol { get; set; }
            public List<Move> Moves { get; set; }

            public Rule(string symbol)
            {
                this.Symbol = symbol;
                this.Moves = new List<Move>();
                this.Moves.Add(new Move(0, symbol));
            }

            public string this[int i]
            {
                get => this.Moves[i].RawValue;
            }
        }

        public class Move
        {
            public string rawValue;

            public string RawValue { get; set; }
            public string NewValue { get; set; }
            public MoveDirection Direction { get; set; }
            public int NewState { get; set; }
            public int CurrentState { get; set; }

            public Move(int currentState, string rawValue)
            {
                this.CurrentState = currentState;
                this.TrySetRawValue(rawValue);
            }

            public bool TrySetRawValue(string rawValue)
            {
                this.RawValue = rawValue;
                if (!this.ParseOnDirection(MoveDirection.Left) && !this.ParseOnDirection(MoveDirection.Right) && !this.ParseOnDirection(MoveDirection.None))
                {
                    this.rawValue = "";
                    return false;
                }
                return true;
            }

            public bool ParseOnDirection(MoveDirection direction)
            {
                var directionChar = GetEnumChar(direction);
                var data = this.RawValue.Split(directionChar);
                if (data.Length == 2 && int.TryParse(data[1], out var newState) && newState <= statesCount)
                {
                    this.Direction = direction;
                    this.NewState = newState;
                    this.NewValue = data[1];
                    return true;
                }
                return false;
            }

            public Move(int currentState)
            {
                this.CurrentState = currentState;
                this.Direction = MoveDirection.None;
                this.NewValue = "";
                this.NewState = currentState;
                this.RawValue = "";
            }

            public override string ToString() => $"{this.NewValue} {GetEnumChar(this.Direction)} {this.NewState}";
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

            VisibleTapeFragment = new ObservableCollection<Cell>(Enumerable.Repeat(new Cell("123"), capacity));

            Rules = new ObservableCollection<Rule>();
            Tape = new CellsTape(VisibleTapeFragment);

            VisibleTapeGrid.ItemsSource = VisibleTapeFragment;
            RulesGrid.ItemsSource = Rules;
            AddRule("");
            AddRuleButton_Click(null, null);
        }

        public void AddRuleButton_Click(object sender, RoutedEventArgs e)
        {
            var column = new DataGridTextColumn();
            column.Header = $"Q{movesCount + 1}";
            column.IsReadOnly = false;
            column.Binding = new Binding(string.Format("[{0}]", movesCount));
            foreach (var rule in Rules)
            {
                rule.Moves.Add(new Move(movesCount));
            }
            RulesGrid.Columns.Add(column);
            movesCount++;
        }

        public void AddStateButton_Click(object sender, RoutedEventArgs e)
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

        public void AddRule(string value)
        {
            var newRule = new Rule(value);
            Rules.Add(newRule);
            Moves.Add(newRule.Moves);
            statesCount++;
        }

        public void StartButton_Click(object sender, RoutedEventArgs e)
        {

        }

        public void ToLeftButton_Click(object sender, RoutedEventArgs e)
        {
            if (Tape.start == 0)
            {
                var newCell = new Cell();
                Tape.tape.Insert(0, newCell);
                Tape.visibleTapeFragment.Insert(0, newCell);
            }
            else
            {
                Tape.visibleTapeFragment.Insert(0, Tape.tape[Tape.start - 1]);
                Tape.start--;
                Tape.finish--;
            }
            Tape.visibleTapeFragment.RemoveAt(capacity);
        }

        public void ToRightButton_Click(object sender, RoutedEventArgs e)
        {
            if (Tape.finish == Tape.tape.Count)
            {
                var newCell = new Cell();
                Tape.tape.Add(newCell);
                Tape.visibleTapeFragment.Add(newCell);
            }
            else
            {
                Tape.visibleTapeFragment.Add(Tape.tape[Tape.finish]);
            }
            Tape.visibleTapeFragment.RemoveAt(0);
            Tape.start++;
            Tape.finish++;
        }

        public void RulesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            e.Cancel = true;

            if ((e?.EditingElement) is TextBox tb)
            {
                var x = e.Column.DisplayIndex;
                var y = e.Row.GetIndex();
                var newText = tb.Text;

                var currentMove = Rules[y].Moves[x];
                if (!currentMove.TrySetRawValue(newText))
                {
                    MessageBox.Show("Wrong value");
                }
            }
        }
    }
}

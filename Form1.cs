namespace _30_Ballbewegen;

using Windows.Gaming.Input;
using Timer = System.Windows.Forms.Timer;
public partial class Form1 : Form
{
    const int PlayerSize = 30;
    const int FormSize = 800;
    const int ShieldSize = 40;

    bool hasShield = false;
    bool shieldAvailable = false;

    int score = 1;
    private static readonly Random rnd = new Random();

    RawGameController? controller = null;

    Panel player = new Panel()
    {
        Size = new Size(PlayerSize, PlayerSize),
        BackColor = Color.SteelBlue
    };

    Timer tmrPollGameControllerInput = new Timer()
    {
        Interval = 16,
        Enabled = true
    };

    Timer timerForLasers = new Timer()
    {
        Interval = 150,
        Enabled = true
    };

    Panel shield = new Panel()
    {
        Size = new Size(ShieldSize, ShieldSize),
        BackColor = Color.YellowGreen
    };

    // Eigene Klasse für einen Laser
    class Laser
    {
        public Rectangle Rect;
        public bool FromRight; // true = kommt von rechts, false = von oben
    }

    readonly List<Laser> allLasers = new List<Laser>();
    Rectangle playerRect;

    public Form1()
    {
        Size = new Size(FormSize, FormSize);
        Text = "Beweg den Ball";
        DoubleBuffered = true;
        InitGame();

        RawGameController.RawGameControllerAdded += RawGameController_RawGameControllerAdded;
        RawGameController.RawGameControllerRemoved += RawGameController_RawGameControllerRemoved;

        tmrPollGameControllerInput.Tick += TmrPollGameControllerInput_Tick;
        timerForLasers.Tick += TimerForLasers_Tick;
    }

    private void TimerForLasers_Tick(object? sender, EventArgs e)
    {
        if (rnd.Next(2) == 1)
        {
            allLasers.Add(new Laser
            {
                Rect = new Rectangle(FormSize, rnd.Next(0, ClientSize.Height - 20), 100, 20),
                FromRight = true
            });
        }
        else
        {
            allLasers.Add(new Laser
            {
                Rect = new Rectangle(rnd.Next(0, ClientSize.Width - 20), -100, 20, 100),
                FromRight = false
            });
        }
    }

    private double NormalizeAxisValue(double value)
    {
        if (double.IsNaN(value)) //Is Not a Number = kein gültiger double Wert
            return 0;

        if (value >= 0 && value <= 1)
            return value * 2 - 1; //0 ... 1 --> -1 ... 1

        if (value < 0)
            return -1;
        if (value > 1)
            return 1;

        return value;
    }

    private void TmrPollGameControllerInput_Tick(object? sender, EventArgs e)
    {
        if (controller != null)
        {
            int numOfAxes = Math.Max(0, controller.AxisCount);
            int numOfButtons = Math.Max(0, controller.ButtonCount);

            double[] axesValues = new double[numOfAxes];
            bool[] buttonValues = new bool[numOfButtons];

            controller.GetCurrentReading(buttonValues, null, axesValues);

            if (numOfAxes > 1) //-1 ... 1
            {
                axesValues[0] = NormalizeAxisValue(axesValues[0]);
                axesValues[1] = NormalizeAxisValue(axesValues[1]);
                double deadzone = 0.2;

                if (axesValues[0] < -deadzone)
                    player.Left -= 7;
                if (axesValues[0] > deadzone)
                    player.Left += 7;
                if (axesValues[1] < -deadzone)
                    player.Top -= 7;
                if (axesValues[1] > deadzone)
                    player.Top += 7;
            }

            if (player.Top <= 0)
                player.Top = 0;
            if (player.Bottom >= ClientSize.Height)
                player.Top = ClientSize.Height - PlayerSize;
            if (player.Left <= 0)
                player.Left = 0;
            if (player.Right >= ClientSize.Width)
                player.Left = ClientSize.Width - PlayerSize;

            List<Laser> copy = new List<Laser>(allLasers);
            foreach (Laser laser in copy)
            {
                if (laser.FromRight)
                    laser.Rect.X -= 4;
                else
                    laser.Rect.Y += 4;

                if (laser.Rect.Right < -5 || laser.Rect.Top > ClientSize.Height + 5)
                {
                    allLasers.Remove(laser);
                    score++;
                }
                playerRect = new Rectangle(player.Left, player.Top, PlayerSize, PlayerSize);
                if (playerRect.IntersectsWith(laser.Rect))
                {
                    if (hasShield)
                    {
                        hasShield = false; // Schild verbraucht
                        allLasers.Remove(laser);
                        player.BackColor = Color.SteelBlue;
                    }
                    else
                        GameOver();
                }
            }

            Invalidate(); // <-- sagt Windows "bitte neu zeichnen"
            if (score % 100 == 0)
                shieldAvailable = true;
            if (shieldAvailable)
            {
                shield.Location = new Point(FormSize - ShieldSize - 25, 10);
                Controls.Add(shield);
            }
            else
            {
                Controls.Remove(shield);
            }

            for (int i = 0; i < buttonValues.Length; i++)
            {
                if (buttonValues[i])
                {
                    string button = $"B{i}";
                    if (button == "B1")
                    {
                        player.BackColor = Color.YellowGreen;
                        hasShield = true;
                        shieldAvailable = false;
                    }
                }
            }
        }
    }

    private void RawGameController_RawGameControllerRemoved(object? sender, RawGameController e)
    {
        controller = null;
    }

    private void RawGameController_RawGameControllerAdded(object? sender, RawGameController e)
    {
        controller = e;
    }

    private void GameOver()
    {
        tmrPollGameControllerInput.Enabled = false;
        timerForLasers.Enabled = false;
        DialogResult result = MessageBox.Show($"Nochmals? \n Score: {score}", "Game Over", MessageBoxButtons.YesNo);
        if (result == DialogResult.Yes)
        {
            InitGame();
        }
        else
        {
            Close();
        }
    }

    private void InitGame()
    {
        foreach (Control item in Controls)
        {
            if (item != player)
            {
                item.Dispose();
            }
        }
        Controls.Clear();
        allLasers.Clear();

        player.Location = new Point(ClientSize.Width / 2 - PlayerSize / 2, ClientSize.Height / 2 - PlayerSize / 2);
        Controls.Add(player);

        score = 1;
        tmrPollGameControllerInput.Enabled = true;
        timerForLasers.Enabled = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.FillRectangle(Brushes.SteelBlue, playerRect);
        foreach (Laser laser in allLasers)
            e.Graphics.FillRectangle(Brushes.Red, laser.Rect);

        // Schild-Icon oben rechts anzeigen wenn verfügbar
        if (shieldAvailable)
            e.Graphics.FillRectangle(Brushes.YellowGreen,
                new Rectangle(ClientSize.Width - ShieldSize - 25, 10, ShieldSize, ShieldSize));

        // Schild um den Spieler anzeigen wenn aktiv
        if (hasShield)
            e.Graphics.DrawRectangle(Pens.YellowGreen,
                new Rectangle(player.Left - 5, player.Top - 5, PlayerSize + 10, PlayerSize + 10));
    }
}

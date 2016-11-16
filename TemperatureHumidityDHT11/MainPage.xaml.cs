using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using System.Diagnostics;

using Windows.Devices.Gpio;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TemperatureHumidityDHT11
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            initializeGpio();
            testClock();
            if (dht11Pin_ != null)
            {
                initializeSensor();
                //initializeTimer();
            }
        }

        private void initializeSensor()
        {
            sensor_ = new DHT11Sensor(dht11Pin_, cronometer_);
            sensor_.initialize();
            float tpr = sensor_.measureTicksPerRead(testPin_, 100);
            float tpw = sensor_.measureUsPerWrite(testPin_, 100);

            textBlockPins.Text = string.Format("Pin read op {0}us, write op {1}us", tpr, tpw);
        }

        //private void initializeTimer()
        //{
        //    timer_ = new DispatcherTimer();
        //    timer_.Interval = TimeSpan.FromSeconds(5);
        //    timer_.Tick += timerTick;
        //    timer_.Start();
        //}

        /// <summary>
        /// 
        /// </summary>
        private bool initializeGpio()
        {
            var gpio = GpioController.GetDefault();

            if (gpio == null)
            {
                textBlockStatus.Text = "Gpio initialization error";
                return false;
            }

            testPin_ = gpio.OpenPin(27);
            testPin_.SetDriveMode(GpioPinDriveMode.Output);
            dht11Pin_ = gpio.OpenPin(5);
            dht11Pin_.SetDriveMode(GpioPinDriveMode.Input);

            textBlockStatus.Text = "Gpio initialized!";
            return true;
        }


        private void testClock()
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

            double d = 123.4567;

            long frequency = Stopwatch.Frequency;
            long nsPerTick = 1000000000 / frequency;
            long ticksPerUs = frequency / 1000000;
            long ticksPerMs = frequency / 1000;

            watch.Start();

            long t1 = watch.ElapsedTicks;
            d = d / 3.33333;
            long t2 = watch.ElapsedTicks;

            textBlockTimer.Text = string.Format("isHighPrec:{0}, ticks/sec={1:N}, ns/tick={2:N}, ticks/us={3:N}, ticks/ms={4:N}, ticks/div op={5:N}, div={6:N}",
                    Stopwatch.IsHighResolution, frequency, nsPerTick, ticksPerUs, ticksPerMs, t2 - t1, d);
        }

        //private void timerTick(object sender, object args)
        //{
        //    textBlockTemperatureHumidity.Text = "Start reading the temperature";
        //    String displayStr = "";
        //    int[] data = new int[4];
        //    PrecisionCronometer c = new PrecisionCronometer();
        //    c.start();
        //    long t = c.ticks;

        //    if (sensor_.read())
        //    {
        //        sensor_.getData(data);
        //        displayStr = string.Format("Temperature:{0}.{1}, humidity:{2}.{3} ({4}us)", 
        //                    data[0], data[1], data[2], data[3], c.ticksToUs(c.ticks - t));
        //    }
        //    else
        //    {
        //        sensor_.getData(data);
        //        displayStr = sensor_.getErrorString() + string.Format(", d[0]={0},d[1]={1},d[2]={2},d[3]={3} ({4}us)", 
        //                    data[0], data[1], data[2], data[3], c.ticksToUs(c.ticks - t));
        //    }
        //    textBlockTemperatureHumidity.Text = displayStr;
        //}


        private void buttonTemperatureHumidity_Click(object sender, RoutedEventArgs e)
        {
            int[] data = new int[4];
            PrecisionCronometer c = new PrecisionCronometer();
            c.start();
            long t = c.ticks;

            if (sensor_ == null)
            {
              textBlockHumidityTemperature.Foreground = new SolidColorBrush(Windows.UI.Colors.Blue);
              textBlockHumidityTemperature.Text = string.Format("Temperature:{0}.{1}, humidity:{2}.{3} ({4}us)",
                            data[0], data[1], data[2], data[3], c.ticksToUs(c.ticks - t));

                return;
            }

            if (sensor_.read())
            {
              sensor_.getData(data);
              textBlockHumidityTemperature.Foreground = new SolidColorBrush(Windows.UI.Colors.Blue);
              textBlockHumidityTemperature.Text = string.Format("Temperature:{0}.{1}, humidity:{2}.{3} ({4}us)",
                            data[0], data[1], data[2], data[3], c.ticksToUs(c.ticks - t));
            }
            else
            {
              sensor_.getData(data);
              textBlockHumidityTemperature.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
              textBlockHumidityTemperature.Text = sensor_.getErrorString() + string.Format(", d[0]={0},d[1]={1},d[2]={2},d[3]={3} ({4}us)",
                            data[0], data[1], data[2], data[3], c.ticksToUs(c.ticks - t));
            }
        }


        private void buttonExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }



  
        //private GpioPin testOutPin_;
        private GpioPin testPin_;
        private GpioPin dht11Pin_;
        private DHT11Sensor sensor_;
        //private DispatcherTimer timer_;
        private PrecisionCronometer cronometer_ = new PrecisionCronometer();

    }
}

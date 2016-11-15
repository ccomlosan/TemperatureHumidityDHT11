
using Windows.Devices.Gpio;

namespace TemperatureHumidityDHT11
{
    class DHT11Sensor
    {
        enum Error
        {
            Sucess = 0,
            StartSignalLowNotSet = 1,
            ReadValueTimeout = 2,
            ReadValueNotDetected = 3

        };
        enum Context
        {
            Null = 0,
            SendStartSignal = 1,
            WaitForAckReadLow = 2,
            WaitForAckReadHigh = 3,
            ReadBitsReadLow = 4,
            ReadBitsReadHigh = 5,

        };

        public DHT11Sensor(GpioPin pin, PrecisionCronometer c)
        {
            pin_ = pin;
            cronometer_ = c;
            cronometer_.start();
        }

        public void initialize()
        {
            cronometer_.start();
        }

        private void reset()
        {
            for (int i = 0; i < SIZE; i++)
            {
                data_[i] = 0;
            }
            error_ = Error.Sucess;
            context_ = Context.Null;
        }

        public void getData(int []data)
        {
            if (data.Length >= SIZE - 1)
            {
                for(int i = 0; i < SIZE - 2; i++)
                {
                    data[i] = data_[i];
                }
            }
        }

        public float measureTicksPerRead(GpioPin testPin, int n)
        {
            testPin.SetDriveMode(GpioPinDriveMode.Input);
            long t1 = cronometer_.ticks;
            for (int i = 1; i < n; i++)
            {
                GpioPinValue val =  testPin.Read();
            }
            long t2 = cronometer_.ticks;
            ticksPerRead_ = t2 - t1;
            usPerRead_ = cronometer_.ticksToUs(ticksPerRead_)/n;
            return usPerRead_;
        }

        public float measureUsPerWrite(GpioPin testPin, int n)
        {
            testPin.SetDriveMode(GpioPinDriveMode.Output);
            long t1 = cronometer_.ticks;
            for (int i = 1; i < n; i++)
            {
                if (i % 2 == 0)
                {
                    testPin.Write(GpioPinValue.Low);
                }
                else
                {
                    testPin.Write(GpioPinValue.High);
                }
            }
            long t2 = cronometer_.ticks;
            ticksPerWrite_ = t2 - t1;
            usPerWrite_ = cronometer_.ticksToUs(ticksPerWrite_)/n;
            return usPerWrite_;
        }


        private Error sendStartSignal()
        {
            pin_.SetDriveMode(GpioPinDriveMode.Output);

            pin_.Write(GpioPinValue.Low);
            cronometer_.wait(18000);//-usPerWrite_

            pin_.Write(GpioPinValue.High);
            cronometer_.wait(30);//-usPerWrite_

            pin_.SetDriveMode(GpioPinDriveMode.Input);

            int count = 0;
            while(count++<100)
            {
                if (pin_.Read() == GpioPinValue.Low)
                {
                    return Error.Sucess;
                }
            }
            context_ = Context.SendStartSignal;
            error_ = Error.StartSignalLowNotSet;
            return error_;
        }

        private Error waitForAcknowledge()
        {
            long dt;

            if (readValue(GpioPinValue.Low, 100, out dt) == Error.Sucess) {
                if (readValue(GpioPinValue.High, 100, out dt) == Error.Sucess) {
                    return Error.Sucess;
                }
                else {
                    context_ = Context.WaitForAckReadHigh;
                }
            }
            else {
                context_ = Context.WaitForAckReadLow;
            }

            return error_;
        }

        private Error readBits()
        {
            long dt;
            int j = 0;
            for (int i = 1; i <= 40; i++)
            {
                if (readValue(GpioPinValue.Low, 70, out dt) == Error.Sucess)
                {
                    j = (i - 1) % 8;
                    data_[j] <<= 1;
                    if (readValue(GpioPinValue.High, 90, out dt) == Error.Sucess)
                    {
                        if (dt >= 60) {
                            data_[j] |= 0x1;
                        }
                    }
                    else
                    {
                        context_ = Context.ReadBitsReadHigh;
                        return error_;
                    }
                }
                else
                {
                    context_ = Context.ReadBitsReadLow;
                    return error_;
                }
                
            }

            return Error.Sucess;
        }


        public bool read()
        {
            reset();
            if(sendStartSignal() == Error.Sucess)
            {
                if (waitForAcknowledge() == Error.Sucess)
                {
                    return readBits() == Error.Sucess && checkData() == true;

                }
            }
            return false;
        }

        public bool checkData()
        {
            return data_[4] == ((data_[3] + data_[2] + data_[1] + data_[0]) & 0xFF);
        }

        private Error readValue(GpioPinValue val, long us, out long dt)
        {
            GpioPinValue lastReadValue;
            int readsNr = 0;
            long tend = cronometer_.getTickToWaitFor(us);
            long tstart = cronometer_.ticks;
            do
            {
                lastReadValue = pin_.Read();
                if (lastReadValue == val)
                {
                    readsNr++;
                }
                else
                {
                    break;
                }
            }
            while (cronometer_.ticks < tend);

            dt = cronometer_.ticksToUs(cronometer_.ticks - tstart);

            if (lastReadValue == val)
            {
                error_ = Error.ReadValueTimeout;
                return error_;
            }
            else
            {
                if (readsNr == 0)
                {
                    error_ = Error.ReadValueNotDetected;
                    return error_;
                }
                return Error.Sucess;
            }
        }

        public string getErrorString()
        {
            return "Error=" + errorStrings[(int)error_] + ", context=" + contextStrings[(int)context_];
        }

        private const int SIZE = 5;
        private GpioPin pin_;
        //private GpioPin testPin_;
        private long ticksPerWrite_ = 0;
        private float usPerWrite_ = 0;
        private long ticksPerRead_ = 0;
        private float usPerRead_ = 0;
        private PrecisionCronometer cronometer_;
        private int[] data_ = new int[SIZE];

        private Error error_ = Error.Sucess;
        private Context context_ = Context.Null;

        static private string []errorStrings = { "Sucess(0)", "StartSignalLowNotSet(1)",
            "ReadValueTimeout(2)", "ReadValueNotDetected(3)"};
        static private string []contextStrings = { "Null(0)", "SendStartSignal(1)", "WaitForAckReadLow(2)",
             "WaitForAckReadHigh(3)", "ReadBitsReadLow(4)", "ReadBitsReadHigh(5)"};
    }
}

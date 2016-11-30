using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TemperatureHumidityDHT11 {
  class DHT11Reader {

    public DHT11Reader(DHT11Sensor sensor) {
      sensor_ = sensor;
    }

    public void start() {
      if (task_ == null) {
        task_ = Task.Factory.StartNew(run, TaskCreationOptions.LongRunning);
      }
    }

    public void stop() {
      lock (lock_) {
        stopSignal_ = true;
      }
      task_.Wait();
    }

    public void wait() {

    }

    private void run() {
      bool shouldStop = false;
      int[] data = new int[4];
      bool hasRead = false;
      do {
        hasRead = readSensor(data);
        lock(lock_) {
          shouldStop = stopSignal_;
          if (hasRead && errorFlag_ == false) {
            setHumidityAndTemperature(data);
          }
        }
        Task.Delay(100).Wait();
      }
      while (shouldStop == false);
    }

    private void setHumidityAndTemperature(int[] data) {
      humidity_ = data[0];
      temperature_ = data[2];
    }

    private bool readSensor(int[] data) {
      DateTime t = DateTime.Now;
      TimeSpan dt = t.Subtract(lastReadTime_);
      double timeout = 3;
      if (errorFlag_) {
        timeout = 1;
      }
      if (dt.TotalSeconds >= timeout) {
        if (sensor_.read()) {
          sensor_.getData(data);          
          errorFlag_ = false;
        }
        else {
          errorFlag_ = true;
        }
        lock(lock_) {
          lastError_ = sensor_.getErrorString();
          lastReadTime_ = t;
        }
        return true;
      }
      return false;
    }

    private Task task_;
    private DHT11Sensor sensor_;
    private float humidity_;
    private float temperature_;
    private DateTime lastReadTime_ = new DateTime();
    private string lastError_ = "";
    private bool errorFlag_ = false;
    private object lock_ = new object();
    private bool stopSignal_ = false;
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TemperatureHumidityDHT11 {
  class DHT11ReadingThread {

    public void start() {

    }

    public void stop() {

    }

    public void wait() {

    }


    private Task task_;
    private DHT11Sensor sensor_;
    private float humidity_;
    private float temperature_;
    private DateTime lastReadTime_;
    private string lastError_;
  }
}

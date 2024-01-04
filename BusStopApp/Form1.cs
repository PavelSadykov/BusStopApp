using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace BusStopApp
{
    public partial class Form1 : Form
    {
        private BusStop busStop;
        private List<Bus> buses = new List<Bus>();
        int passengersOnBoard;
        public Form1()
        {
            InitializeComponent();
            busStop = new BusStop(40, 20); // начальное количество пассажиров и вместимость автобуса

            // Создаем  автобусы с разными номерами.
            for (int i = 1; i <= 3; i++) 
            {
                buses.Add(new Bus(busStop, i));
            }
        }

        private void btnStart_Click_1(object sender, EventArgs e)
        {
            Thread stopThread = new Thread(() =>
            {
                busStop.Run((message) =>
                {
                    UpdateTextBox(message);
                });
            });
            stopThread.IsBackground = true;
            stopThread.Start();

            Thread busThread = new Thread(() =>
            {
                Random random = new Random();
                while (true)
                {
                    passengersOnBoard = random.Next(0, 21); // Случаное количество пассажиров в автобусе
                    Thread.Sleep(random.Next(3000, 10000)); // Случайное время интервала прибытия автобусов
                    foreach (var bus in buses)
                    {
                        bus.Arrive(passengersOnBoard);
                    }
                }
            });
            busThread.IsBackground = true;
            busThread.Start();

            MessageBox.Show("Симуляция запущена.");
        }
        // Метод для безопасного обновления текстового поля на форме из другого потока
        private void UpdateTextBox(string message)
        {
            if (textBox1.InvokeRequired)
            {
                textBox1.Invoke(new Action<string>(UpdateTextBox), new object[] { message });
            }
            else
            {
                textBox1.AppendText(message + Environment.NewLine);
            }
        }

        private void btnArrive_Click_1(object sender, EventArgs e)
        {

            foreach (var bus in buses)
            {
                bus.Arrive(passengersOnBoard);
            }
        }
    }

    public class BusStop
    {   public object lockObj = new object();
        private Dictionary<int,int> passengersByBusNumber = new Dictionary<int,int>();  
        private int peopleAtStop; // количество людей на остановке
        private int busCapacity; // вместимость автобуса
       

        // Событие для сигнализации о прибытии автобуса
        private AutoResetEvent busArrival = new AutoResetEvent(false);

        public BusStop(int initialPeople, int capacity)
        {
            peopleAtStop = initialPeople;
            busCapacity = capacity;
        }

        public void Run(Action<string> messageCallback)
        {
            Random random = new Random();

            while (true)
            {
                int waitingPassengers = random.Next(1, 11); // случайное число людей на остановке
                messageCallback?.Invoke($"На остановку пришло {waitingPassengers} пассажиров.");

                lock (lockObj)
                {
                    for (int i = 0; i < waitingPassengers; i++)
                    {
                        int passengerBusNumber = random.Next(1, 4); // Случайный номер автобуса
                        if (passengersByBusNumber.ContainsKey(passengerBusNumber))
                        {
                            passengersByBusNumber[passengerBusNumber]++;
                        }
                        else
                        {
                            passengersByBusNumber.Add(passengerBusNumber, 1);
                        }
                        messageCallback?.Invoke($"На остановку пришло:{passengersByBusNumber[passengerBusNumber]} пассажиров, ожидающих автобус №{passengerBusNumber}. ");
                    }



                      peopleAtStop += waitingPassengers; // добавляем новых пассажиров на остановку
                }

                messageCallback?.Invoke($"Текущее количество пассажиров на остановке: {peopleAtStop}");
                // Имитация времени между прибытием автобусов
                Thread.Sleep(random.Next(1000, 5000));
                // Ожидание прибытия автобуса
                busArrival.WaitOne();

                lock (lockObj)
                {
                    int passengersToBoard = Math.Min(peopleAtStop, busCapacity); // число пассажиров, которые могут уехать на автобусе
                    peopleAtStop -= passengersToBoard; // высаживаем пассажиров из автобуса
                    messageCallback?.Invoke($"Автобус уехал, пассажиров на остановке осталось: {peopleAtStop}");
                }

                
            }
        }

        public int GetPassengersForBus(int busNumber)
        {
            lock (lockObj)
            {
                return passengersByBusNumber.ContainsKey(busNumber) ? passengersByBusNumber[busNumber] : 0;
            }
        }

        public int GetPeopleAtStop()
        {
            return peopleAtStop;
        }
        public void UpdatePeopleAtStop(int passengersToBoard)
        {
            peopleAtStop -= passengersToBoard;
        }

        public void BusArrived()
        {
            busArrival.Set(); // сигнал для автобуса о прибытии
        }
    }

    public class Bus
    {
        private int busNumber;
        private BusStop busStop;

        public Bus(BusStop stop, int number)
        {
            busStop = stop;
            busNumber = number;
        }

        public void Arrive(int passengersOnBoard)
        {
            MessageBox.Show($"Автобус №{busNumber} приехал на остановку с {passengersOnBoard} пассажирами на борту.");

            int passengersToBoard = Math.Min(passengersOnBoard, busStop.GetPeopleAtStop());

            lock (busStop.lockObj) 
            {
                busStop.UpdatePeopleAtStop(passengersToBoard);
                Thread.Sleep(2000);

                MessageBox.Show($"Автобус №{busNumber} взял {passengersToBoard} пассажиров.");
                Thread.Sleep(3000);
                if (passengersToBoard < passengersOnBoard)
                {
                    Thread.Sleep(2000);
                    MessageBox.Show($"Оставшиеся пассажиры уедут на следующем автобусе.");
                }
            }
        }
    }
}
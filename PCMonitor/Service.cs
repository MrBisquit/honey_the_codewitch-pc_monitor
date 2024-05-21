using OpenHardwareMonitor.Hardware;

using PCMonitor;

using System;
using System.IO;
using System.IO.Ports;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using Timer = System.Timers.Timer;
namespace PCMonitor
{
	partial class Service : ServiceBase
	{
		const int Baud = 115200;
		Timer _updateTimer;
		private readonly Computer _computer = new Computer
		{
			CPUEnabled = true,
			GPUEnabled = true
		};
		// traverses OHWM data
		public class UpdateVisitor : IVisitor
		{
			public void VisitComputer(IComputer computer)
			{
				computer.Traverse(this);
			}
			public void VisitHardware(IHardware hardware)
			{
				hardware.Update();
				foreach (IHardware subHardware in hardware.SubHardware)
					subHardware.Accept(this);
			}
			public void VisitSensor(ISensor sensor) { }
			public void VisitParameter(IParameter parameter) { }
		}
		ISensor FindSensor(string identifier)
		{
			//try
			//{
				return FindSensor(identifier, _computer);
			//}
			//catch { }
			//return null;
		}
		static ISensor FindSensor(string identifier, object current)
		{
			
			if (current == null || string.IsNullOrEmpty(identifier))
			{
				return null;
			}
			if (current is IComputer)
			{
				var comp = (IComputer)current;
				foreach (IHardware hardware in comp.Hardware)
				{
					var result = FindSensor(identifier, hardware);
					if (result != null)
					{
						return result;
					}
				}
			}
			else if (current is IHardware)
			{
				var hardware = (IHardware)current;
				foreach (ISensor result in hardware.Sensors)
				{
					if (result.Identifier.ToString() == identifier)
					{
						return result;
					}
				}
				foreach (IHardware subhardware in hardware.SubHardware)
				{
					var result = FindSensor(identifier, subhardware);
					if (result != null)
					{
						return result;
					}
				}
			}
			return null;
		}
		float GetSensorValue(ConfigEntry entry)
		{
			var sensor = FindSensor(entry.Path);
			if (sensor == null)
			{
				return float.NaN;
			}
			if (!sensor.Value.HasValue)
			{
				return float.NaN;
			}
			return sensor.Value.GetValueOrDefault();
		}
		Config[] _configs = null;
		public Service()
		{
			InitializeComponent();
		}
		public static void StructToBytes(object value, byte[] data, int startIndex)
		{
			var size = Marshal.SizeOf(value.GetType());
			var ptr = Marshal.AllocHGlobal(size);
			Marshal.StructureToPtr(value, ptr, false);
			var ba = new byte[size];
			Marshal.Copy(ptr, data, startIndex, size);
			Marshal.FreeHGlobal(ptr);
		}
		protected override void OnStart(string[] args)
		{
			JsonWatcher.Renamed += JsonWatcher_Renamed;
			JsonWatcher.Changed += JsonWatcher_Changed;
			JsonWatcher.Created += JsonWatcher_Created;
			JsonWatcher.Deleted += JsonWatcher_Deleted;
			_updateTimer = new Timer(100);
			_updateTimer.Elapsed += _UpdateTimer_Elapsed;

			var fullpath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName), "pcmon.json");
			if (File.Exists(fullpath)) {
				LoadConfigs(fullpath);
			}
			CollectSystemInfo();
			JsonWatcher.EnableRaisingEvents = true;

		}

		private void _UpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			// use OpenHardwareMonitorLib to collect the system info
			var updateVisitor = new UpdateVisitor();
			_computer.Accept(updateVisitor);
			
			// go through all the ports
			int i = 0;
			if (_configs != null)
			{
				foreach (var cfg in _configs)
				{
					SerialPort port = cfg.Port;
					if (port != null)
					{
						try
						{
							if (!port.IsOpen)
							{
								port.Open();
								//Log.AppendText("Opened " + cfg.PortName + " (lazy open)" + Environment.NewLine);
								//port.DataReceived += Port_DataReceived;
							}
							ScreenPacket data = default;
							int psize = Marshal.SizeOf(data);
							var ba = new byte[2 + (psize * cfg.Entries.Count)];
							ba[0] = 1;
							ba[1] = (byte)cfg.Entries.Count;
							int si = 2;
							for (int j = 0; j < cfg.Entries.Count; ++j)
							{
								// put it in the struct for sending
								ConfigEntry cfge = cfg.Entries[j];
								data.Format = cfge.Format;
								data.ValueMax = cfge.ValueMax;
								data.Icon = cfge.IconData;
								data.Colors = new ushort[2];
								data.Colors[0] = cfge.ColorStartRgb565;
								data.Colors[1] = cfge.ColorEndRgb565;
								data.HsvColor = cfge.ColorHsv;
								data.Value = GetSensorValue(cfge);
								StructToBytes(data, ba, si);
								si += psize;
							}
							port.Write(ba, 0, ba.Length);
							port.BaseStream.Flush();
						}
						catch { }
					}
					++i;
				}
			}
		}

		

		void DeleteConfigs()
		{
			_updateTimer.Enabled = false;
			if (_configs != null)
			{
				foreach (var cfg in _configs)
				{
					try
					{
						if (cfg.PortIfCreated != null)
						{
							if (cfg.PortIfCreated.IsOpen)
							{
								cfg.PortIfCreated.Close();
							}
						}

					}
					catch { }
				}
				_configs = new Config[0];
				try
				{
					_computer.Close();
				}
				catch
				{

				}
			}
		}
		float cpuUsage;
		string cpuUsageId;
		float gpuUsage;
		string gpuUsageId;
		float cpuTemp;
		string cpuTempId;
		float cpuTjMax;
		float gpuTemp;
		string gpuTempId;
		bool isNvidia;
		void CollectSystemInfo()
		{
			// use OpenHardwareMonitorLib to collect the system info
			var updateVisitor = new UpdateVisitor();
			_computer.Accept(updateVisitor);
			isNvidia = false;
			for (int i = 0; i < _computer.Hardware.Length; i++)
			{
				if (_computer.Hardware[i].HardwareType == HardwareType.CPU)
				{
					for (int j = 0; j < _computer.Hardware[i].Sensors.Length; j++)
					{
						var sensor = _computer.Hardware[i].Sensors[j];
						if (sensor.SensorType == SensorType.Temperature &&
							sensor.Name.Contains("CPU Package"))
						{
							for (int k = 0; k < sensor.Parameters.Length; ++k)
							{
								var p = sensor.Parameters[i];
								if (p.Name.ToLowerInvariant().Contains("tjmax"))
								{
									cpuTjMax = (float)p.Value;
								}
							}
							cpuTemp = sensor.Value.GetValueOrDefault();
							cpuTempId = sensor.Identifier.ToString();
						}
						else if (sensor.SensorType == SensorType.Load &&
							sensor.Name.Contains("CPU Total"))
						{
							// store
							cpuUsage = sensor.Value.GetValueOrDefault();
							cpuUsageId = sensor.Identifier.ToString();
						}
					}
				}
				if (_computer.Hardware[i].HardwareType == HardwareType.GpuAti ||
					_computer.Hardware[i].HardwareType == HardwareType.GpuNvidia)
				{
					isNvidia = _computer.Hardware[i].HardwareType == HardwareType.GpuNvidia;
					for (int j = 0; j < _computer.Hardware[i].Sensors.Length; j++)
					{
						var sensor = _computer.Hardware[i].Sensors[j];
						if (sensor.SensorType == SensorType.Temperature &&
							sensor.Name.Contains("GPU Core"))
						{
							// store
							gpuTemp = sensor.Value.GetValueOrDefault();
							gpuTempId = sensor.Identifier.ToString();
						}
						else if (sensor.SensorType == SensorType.Load &&
							sensor.Name.Contains("GPU Core"))
						{
							// store
							gpuUsage = sensor.Value.GetValueOrDefault();
							gpuUsageId = sensor.Identifier.ToString();
						}
					}
				}
			}
		}
		void LoadConfigs(string fullpath)
		{
			DeleteConfigs();
			try
			{
				using (var reader = new StreamReader(fullpath))
				{
					_configs = Config.ReadFrom(reader);
				}
				foreach(var cfg in _configs)
				{
					cfg.Port = new SerialPort(cfg.PortName, Baud);
				}
				_computer.Open();
				_updateTimer.Enabled = true;
			}
			catch
			{

			}
		}
		private void JsonWatcher_Deleted(object sender, System.IO.FileSystemEventArgs e)
		{
			if(e.Name=="pcmon.json")
			{
				DeleteConfigs();
			}
		}

		private void JsonWatcher_Created(object sender, System.IO.FileSystemEventArgs e)
		{
			if (e.Name == "pcmon.json")
			{
				LoadConfigs(e.FullPath);
			}
		}

		private void JsonWatcher_Changed(object sender, System.IO.FileSystemEventArgs e)
		{
			LoadConfigs(e.FullPath);
		}

		private void JsonWatcher_Renamed(object sender, System.IO.RenamedEventArgs e)
		{
			if(e.OldName=="pcmon.json" && e.Name!="pcmon.json")
			{
				DeleteConfigs();
			} else if(e.OldName!="pcmon.json" && e.Name=="pcmon.json")
			{
				LoadConfigs(e.FullPath);
			}
			
		}

		protected override void OnStop()
		{
			JsonWatcher.EnableRaisingEvents = false;
		}
	}
}

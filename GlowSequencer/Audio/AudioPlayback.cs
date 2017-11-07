using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.Audio
{
    public class AudioPlayback : IDisposable
    {
        private WaveOut playbackDevice;
        private AudioFileReader fileStream;
        
        //public event EventHandler<FftEventArgs> FftCalculated;
        //public event EventHandler<MaxSampleEventArgs> MaximumCalculated;

        public ISampleProvider Stream => fileStream;
        public bool IsPlaying => playbackDevice != null && playbackDevice.PlaybackState == PlaybackState.Playing;

        public void Load(string fileName)
        {
            Stop();
            CloseFile();
            EnsureDeviceCreated();
            OpenFile(fileName);
        }

        private void CloseFile()
        {
            fileStream?.Dispose();
            fileStream = null;
        }

        private void OpenFile(string fileName)
        {
            try
            {
                fileStream = new AudioFileReader(fileName);

                playbackDevice.Init(fileStream);
                
                //var aggregator = new SampleAggregator(inputStream);
                //aggregator.NotificationCount = inputStream.WaveFormat.SampleRate / 100;
                ////aggregator.PerformFFT = true;
                ////aggregator.FftCalculated += (s, a) => FftCalculated?.Invoke(this, a);
                //aggregator.MaximumCalculated += (s, a) => MaximumCalculated?.Invoke(this, a);
                //playbackDevice.Init(aggregator);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message, "Problem opening file");
                CloseFile();
            }
        }

        private void EnsureDeviceCreated()
        {
            if (playbackDevice == null)
            {
                CreateDevice();
            }
        }

        private void CreateDevice()
        {
            playbackDevice = new WaveOut { DesiredLatency = 200 };
        }

        public void Play()
        {
            if (playbackDevice != null && fileStream != null && playbackDevice.PlaybackState != PlaybackState.Playing)
            {
                playbackDevice.Play();
            }
        }

        public void Pause()
        {
            playbackDevice?.Pause();
        }

        public void Seek(TimeSpan time)
        {
            fileStream.CurrentTime = time;
        }

        public void Stop()
        {
            playbackDevice?.Stop();
            if (fileStream != null)
            {
                fileStream.Position = 0;
            }
        }

        public void Dispose()
        {
            Stop();
            CloseFile();
            playbackDevice?.Dispose();
            playbackDevice = null;
        }
    }
}

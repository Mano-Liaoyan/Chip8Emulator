using System;
using OpenTK.Audio.OpenAL;

namespace Chip8Emulator.Core;

public class SoundPlayer : IDisposable
{
    private readonly int _buffer;
    private readonly ALContext _context;
    private readonly ALDevice _device;
    private readonly int _source;
    private bool _isPlaying;

    public SoundPlayer()
    {
        // 1. Open Device
        string defaultDeviceName = ALC.GetString(ALDevice.Null, AlcGetString.DefaultDeviceSpecifier);
        _device = ALC.OpenDevice(defaultDeviceName);

        // 2. Create Context
        _context = ALC.CreateContext(_device, Array.Empty<int>());
        ALC.MakeContextCurrent(_context);

        // 3. Generate Sound Data (Simple beep)
        // Sine wave: standard beep
        const int sampleRate = 44100;
        const double frequency = 440.0; // A4
        const int lengthMs = 1000; // 1 second buffer, we will loop it
        const int dataCount = sampleRate * lengthMs / 1000;
        short[] bufferData = new short[dataCount];

        for (int i = 0; i < dataCount; i++)
        {
            double angle = Math.PI * 2.0 * frequency * i / sampleRate;
            // Amplitude 0.5 * short.MaxValue
            bufferData[i] = (short)(Math.Sin(angle) * (short.MaxValue * 0.5));
        }

        // 4. Create OpenAL Buffer
        _buffer = AL.GenBuffer();
        AL.BufferData(_buffer, ALFormat.Stereo16, bufferData, sampleRate);

        // 5. Create OpenAL Source
        _source = AL.GenSource();
        AL.Source(_source, ALSourcei.Buffer, _buffer);
        AL.Source(_source, ALSourceb.Looping, true);
    }

    public void Dispose()
    {
        Stop();
        AL.DeleteSource(_source);
        AL.DeleteBuffer(_buffer);
        ALC.DestroyContext(_context);
        ALC.CloseDevice(_device);
        GC.SuppressFinalize(this);
    }

    public void Play()
    {
        if (_isPlaying) return;

        AL.SourcePlay(_source);
        _isPlaying = true;
    }

    public void Stop()
    {
        if (!_isPlaying) return;

        AL.SourceStop(_source);
        _isPlaying = false;
    }
}
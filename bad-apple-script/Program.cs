using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        const int WIDTH = 96;
        const char BLACK = '';
        const char WHITE = '';

        IMyTextSurface surface;
        IMyTextSurface info;

        int frame = 0;
        int delayCounter = 0;
        string[] frames;
        List<uint> frameBuffer = new List<uint>();

        public Program()
        {
            // Get the LCDs
            var block = GridTerminalSystem.GetBlockWithName("Bad Apple LCD") as IMyTextSurfaceProvider;
            if (block == null)
                throw new Exception("LCD named 'Bad Apple LCD' not found");
            surface = block.GetSurface(0);
            info = Me.GetSurface(0);
            Echo("Found LCD.");

            // Split the frames on newlines.
            Echo("Reading Frames.");
            frames = Me.CustomData.Split(Environment.NewLine.ToCharArray());


            Echo($"Found {frames.Length} frames.");
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void DrawFrame(ref List<uint> frame, IMyTextSurface surface)
        {
            int x = 0;
            bool black = true;
            var sb = new StringBuilder();

            for (int fi = 0; fi < frame.Count; fi++)
            {
                for (int i = 0; i < frame[fi]; i++)
                {
                    // Draw the black or white character.
                    sb.Append(black ? BLACK : WHITE);

                    // Add a newline every 96 characters.
                    x++;
                    if (x == WIDTH)
                    {
                        sb.AppendLine();
                        x = 0;
                    }
                }
                black = !black;
            }
            surface.WriteText(sb);
        }
        public void DecodeFrame(int frame, ref List<uint> buffer)
        {
            const uint OFFSET = 0x00B0;
            buffer.Clear();
            foreach (var ch in frames[frame])
            {
                buffer.Add(ch - OFFSET);
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (delayCounter == 0)
            {
                info.WriteText($"F {frame}\nT {frame / 30.0:00.00}");
                DecodeFrame(frame, ref frameBuffer);
            }
            else if (delayCounter == 1)
            {
                DrawFrame(ref frameBuffer, surface);
                frame++;

                if (frame >= frames.Length)
                {
                    frame = 0;
                }
            }

            delayCounter++;
            if (delayCounter > 1)
                delayCounter = 0;
        }
    }
}

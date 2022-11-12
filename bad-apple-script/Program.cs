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

        IMyTextSurface screen;
        IMyTextSurface info;

        IMySoundBlock sound;

        int frame = 0;
        List<string> frames;

        StringBuilder sb = new StringBuilder();

        List<uint> frameBuffer = new List<uint>();
        IEnumerator<bool> job;

        public Program()
        {
            // Get the LCDs
            var block = GridTerminalSystem.GetBlockWithName("Bad Apple LCD") as IMyTextSurfaceProvider;
            if (block == null)
                throw new Exception("LCD named 'Bad Apple LCD' not found");
            screen = block.GetSurface(0);
            screen.WriteText("");

            // Get the info screen.
            info = Me.GetSurface(0);
            info.WriteText("");

            // Get the sound block.
            sound = GridTerminalSystem.GetBlockWithName("Bad Apple Sound") as IMySoundBlock;
            if (sound == null)
                throw new Exception("No Sound Block named 'Bad Apple Sound'");

            // Get the storage blocks.
            var group = GridTerminalSystem.GetBlockGroupWithName("Bad Apple Memory");
            if (group == null)
                throw new Exception("No Group named 'Bad Apple Memory'");
            var storageBlocks = new List<IMyTimerBlock>();
            group.GetBlocksOfType(storageBlocks);

            // Order the blocks by Name.
            storageBlocks.OrderBy(o => o.CustomName).ToList();

            Echo($"Storage blocks: {storageBlocks.Count}");

            // Start the loader.
            job = Loader(storageBlocks);
            Runtime.UpdateFrequency = UpdateFrequency.Once;


        }

        public void Main(string argument, UpdateType updateSource)
        {
            if ((updateSource & (UpdateType.Terminal | UpdateType.Trigger)) != 0 && job == null)
            {
                Runtime.UpdateFrequency |= UpdateFrequency.Once;
                job = Player();
            }
            // Are we updating every tick?
            if ((updateSource & UpdateType.Once) != 0 && job != null)
            {
                if (job.MoveNext())
                    Runtime.UpdateFrequency |= UpdateFrequency.Once;
                else
                {
                    job.Dispose();
                    job = null;
                }
            }
        }

        public void DrawFrame(ref List<uint> frame, IMyTextSurface surface)
        {
            int x = 0;
            bool black = true;
            sb.Clear();

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
        public void DecodeFrame(ref List<uint> buffer)
        {
            // Decode the raw frame into the buffer.
            const uint OFFSET = 0x00B0;
            buffer.Clear();
            for (int i = 0; i < frames[frame].Length; i++)
                buffer.Add(frames[frame][i] - OFFSET);
        }
        public IEnumerator<bool> Player()
        {
            // Stop the sound block and clear the screen.
            sound.Stop();
            screen.WriteText("");

            // Wait 1 second.
            for (int i = 0; i < 60; i++)
                yield return true;

            // Play the sound block.
            sound.Play();

            frame = 0;
            while (frame < frames.Count)
            {
                // Decode the next frame.
                DecodeFrame(ref frameBuffer);
                info.WriteText($"Frame {frame}\nTime {frame / 30.0:00.00}");
                yield return true;

                // Draw whatever's in the buffer.
                DrawFrame(ref frameBuffer, screen);
                frame++;
                yield return true;
            }
        }

        public IEnumerator<bool> Loader(List<IMyTimerBlock> memoryBlocks)
        {
            // Draw the VLC icon (caus why not!).
            screen.WriteText(VLC_ICON);
            yield return true;

            // Init our memory.
            LogLn("Memory Init");

            frames = new List<string>();
            var hashes = new int[memoryBlocks.Count];

            IMyTimerBlock memoryBlock = null;

            for (int i = 0; i < memoryBlocks.Count; i++)
            {
                memoryBlock = memoryBlocks[i];

                // Verify the custom data of this block doesn't already exist.
                {
                    var hash = memoryBlock.CustomData.GetHashCode();
                    if (hashes.Contains(hash))
                        LogLn($"WANING: {memoryBlock.CustomData}'s data already exists.");
                    hashes[i] = hash;
                }

                Log(memoryBlock.CustomName);

                // Get all the lines from this block's custom data.
                var lines = memoryBlock.CustomData.Split(Environment.NewLine.ToCharArray());

                // Log the first comment if one exists.
                if (lines[0].StartsWith("#"))
                    Log($" {lines[0]}");
                else
                    Log(" Unknown");

                // Add each line to our frames that don't start with # (skip comments).
                foreach (var line in lines)
                    if (!line.StartsWith("#"))
                        frames.Add(line);

                LogLn($" [{lines.Length:000}]");
                yield return true;
            }
            LogLn($"Loaded {frames.Count()} frames");
            Echo($"Loaded {frames.Count()} frames");
            LogLn($"from {memoryBlocks.Count()} memory blocks.");
            Echo($"from {memoryBlocks.Count()} memory blocks.");
        }

        void Log(string msg)
        {
            if (info != null)
                info.WriteText(msg, true);
        }
        void LogLn(string msg)
        {
            if (info != null)
                info.WriteText(msg + '\n', true);
        }

        const string VLC_ICON = "\n\n\n\n\n\n\n\n\n\n" + "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n";
    }
}

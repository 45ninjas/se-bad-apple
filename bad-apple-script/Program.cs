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
        const int HASHRATE = 250000;
        const int WIDTH = 96;
        const char BLACK = '';
        const char WHITE = '';

        IMyTextSurface surface;
        IMyTextSurface info;

        IMySoundBlock sound;

        int frame = 0;
        List<string> frames;

        List<uint> frameBuffer = new List<uint>();
        IEnumerator<bool> job;

        public Program()
        {
            // Get the LCDs
            var block = GridTerminalSystem.GetBlockWithName("Bad Apple LCD") as IMyTextSurfaceProvider;
            if (block == null)
                throw new Exception("LCD named 'Bad Apple LCD' not found");
            surface = block.GetSurface(0);
            surface.WriteText("");

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
        public void DecodeFrame(ref List<uint> buffer)
        {
            // Decode the raw frame into the buffer.
            const uint OFFSET = 0x00B0;
            buffer.Clear();
            foreach (var ch in frames[frame])
            {
                buffer.Add(ch - OFFSET);
            }
        }
        public IEnumerator<bool> Player()
        {
            // Stop the sound block and clear the screen.
            sound.Stop();
            surface.WriteText("");

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
                DrawFrame(ref frameBuffer, surface);
                frame++;
                yield return true;
            }
        }

        public IEnumerator<bool> Loader(List<IMyTimerBlock> memoryBlocks) {
            // Draw the VLC icon (caus why not!).
            surface.WriteText(VLC_ICON);
            yield return true;

            // Init our memory.
            surface.WriteText("\n\nMemory Init.", true);

            frames = new List<string>();
            
            // Go over each memory block in the group.
            foreach (var memoryBlock in memoryBlocks)
            {
                surface.WriteText($"\n{memoryBlock.CustomName}", true);

                var lines = memoryBlock.CustomData.Split(Environment.NewLine.ToCharArray());

                foreach (var line in lines)
                    frames.Add(line);

                surface.WriteText($" [{lines.Length:000}]", true);
                yield return true;
                yield return true;
                yield return true;
                yield return true;
            }
        }

        const string VLC_ICON = "\n\n\n\n\n\n\n\n\n\n" + "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n";
    }
}

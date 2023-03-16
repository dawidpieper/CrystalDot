using System;
using System.Windows.Media;
using System.Linq;
using System.Collections.Generic;
using g3;
using System.IO;

namespace CrystalDot
{

    public enum Alignment
    {
        Left,
        Center,
        Right
    }

    public class Signage
    {
        public string Text = "";
        public LibLouis.BrailleTable BrailleTable = new LibLouis.BrailleTable("");
        public Decimal Height = 2;
        public Decimal LeftMargin = 3;
        public Decimal RightMargin = 3;
        public Decimal UpperMargin = 0;
        public Decimal BottomMargin = 0;
        public Decimal DotBase = 1.5m;
        public Decimal DotWidth = 2.5m;
        public Decimal DotHeight = 0.5m;
        public Decimal CharacterWidth = 6;
        public Decimal LineHeight = 10;
        public Alignment BrailleAlignment = Alignment.Center;

        public Signage() { }

        public byte[][] GetDots()
        {
            string[] lines = Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            byte[][] r = new byte[lines.Count()][];
            for (int i = 0; i < lines.Count(); ++i)
                r[i] = LibLouis.BrailleToDots(BrailleTable.Location, LibLouis.TranslateToBraille(BrailleTable.Location, lines[i]));
            return r;
        }

        public Decimal GetWidth()
        {
            byte[][] dots = GetDots();
            int chars = 0;
            for (int i = 0; i < dots.Count(); ++i) if (dots[i].Count() > chars) chars = dots[i].Count();
            return chars * CharacterWidth + LeftMargin + RightMargin;
        }

        public Decimal GetHeight()
        {
            string[] lines = Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            Decimal h = lines.Count() * LineHeight;
            return h + UpperMargin + BottomMargin;
        }

        public (Decimal, Decimal)[] GetDotLocations()
        {
            string[] lines = Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            byte[][] brailleLines = new byte[lines.Count()][];
            for (int i = 0; i < lines.Count(); ++i)
                brailleLines[i] = LibLouis.BrailleToDots(BrailleTable.Location, LibLouis.TranslateToBraille(BrailleTable.Location, lines[i]));
            var points = new List<(Decimal, Decimal)>();

            int chars = 0;
            for (int i = 0; i < brailleLines.Count(); ++i) if (brailleLines[i].Count() > chars) chars = brailleLines[i].Count();
            Decimal w = chars * CharacterWidth;
            Decimal h = lines.Count() * LineHeight;

            for (int i = 0; i < brailleLines.Count(); ++i)
            {
                var line = brailleLines[i];
                Decimal lineW = line.Count() * CharacterWidth;
                Decimal x = LeftMargin;
                if (BrailleAlignment == Alignment.Center)
                    x = LeftMargin + w / 2 - lineW / 2;
                if (BrailleAlignment == Alignment.Right)
                    x = LeftMargin + w - lineW;
                Decimal y = h - LineHeight * i;
                x += CharacterWidth / 2;
                y -= LineHeight / 2;

                foreach (var c in line)
                {
                    for (int dot = 0; dot < 8; ++dot)
                    {
                        if ((c & (1 << dot)) > 0)
                        {
                            Decimal dx = -DotWidth / 2 + Math.Floor(((Decimal)dot / 3)) * DotWidth;
                            Decimal dy = DotWidth - (((dot % 3)) * DotWidth);
                            points.Add((x + dx, y + dy));
                        }
                    }
                    x += CharacterWidth;
                }

            }

            return points.ToArray();
        }

        public DMesh3 GenerateMesh()
        {
            DMesh3Builder builder = new DMesh3Builder();
            var signageGenerator = new GridBox3Generator();
            var signage = signageGenerator.Generate().MakeDMesh();
            MeshTransforms.Translate(signage, 0.5, 0.5, 0.5);
            MeshTransforms.Scale(signage, (double)GetWidth(), (double)GetHeight(), (double)Height);
            builder.AppendNewMesh(signage);
            var dotGenerator = new Sphere3Generator_NormalizedCube();
            dotGenerator.Radius = (double)DotBase / 2;
            var points = GetDotLocations();
            foreach ((Decimal x, Decimal y) in points)
            {
                var sphere = dotGenerator.Generate().MakeDMesh();
                MeshTransforms.Scale(sphere, 1.0, 1.0, (double)DotHeight * 2 / (double)DotBase);
                MeshTransforms.Translate(sphere, (double)x, (double)y, (double)Height);
                builder.AppendNewMesh(sphere);
            }

            var mesh = MeshEditor.Combine(builder.Meshes.ToArray());

            return mesh;
        }

        public void WriteMesh(string file)
        {
            IOWriteResult result = StandardMeshWriter.WriteFile(file, new List<WriteMesh>() { new WriteMesh(GenerateMesh()) }, WriteOptions.Defaults);
        }

    }
}
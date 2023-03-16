using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;
using System.Windows.Threading;

namespace CrystalDot
{
    public partial class MainWindow : Window
    {

        private LibLouis.BrailleTable[] _BrailleTables;

        public MainWindow()
        {
            InitializeComponent();
            _BrailleTables = LibLouis.GetTables();
            LanguageComboBox.ItemsSource = _BrailleTables;
            LanguageComboBox.SelectedItem = _BrailleTables.FirstOrDefault(b => b.Language != null && b.Language.ToUpper().Equals(CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToUpper()));

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (sender, e) => Update();
            timer.Start();
        }

        public Signage GenerateSignage()
        {
            var signage = new Signage();
            signage.Text = SignageTextBox.Text;
            signage.BrailleTable = (LibLouis.BrailleTable)LanguageComboBox.SelectedItem;
            signage.Height = (Decimal)SignageHeightSlider.Value;
            signage.LeftMargin = (Decimal)LeftMarginSlider.Value;
            signage.RightMargin = (Decimal)RightMarginSlider.Value;
            signage.UpperMargin = (Decimal)UpperMarginSlider.Value;
            signage.BottomMargin = (Decimal)BottomMarginSlider.Value;
            signage.DotBase = (Decimal)DotBaseSlider.Value;
            signage.DotWidth = (Decimal)DotWidthSlider.Value;
            signage.DotHeight = (Decimal)DotHeightSlider.Value;
            signage.CharacterWidth = (Decimal)CharacterWidthSlider.Value;
            signage.LineHeight = (Decimal)LineHeightSlider.Value;
            signage.BrailleAlignment = Alignment.Center;
            if (BrailleAlignmentComboBox.SelectedIndex == 0) signage.BrailleAlignment = Alignment.Left;
            if (BrailleAlignmentComboBox.SelectedIndex == 2) signage.BrailleAlignment = Alignment.Right;
            return signage;
        }

        public void Update()
        {
            var signage = GenerateSignage();
            Decimal w = signage.GetWidth();
            Decimal h = signage.GetHeight();
            SignageSizeLabel.Content = w.ToString() + " x " + h.ToString() + " mm";
        }

        private void AboutButton_Click(object sender, EventArgs e)
        {
            string str = @"CrystalDot
Simple braille signages generator

Copyright (©) Dawid Pieper

CrystalDot is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, version 3.
CrystalDot is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
CrystalDot source code can be found at GitHub:
https://github.com/dawidpieper/CrystalDot";
            MessageBoxResult result = MessageBox.Show(str,
                                                      "CrystalDot",
                                                      MessageBoxButton.OK,
                                                      MessageBoxImage.Information);
        }

        private void GenerateButton_Click(object sender, EventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "signage.stl";
            dlg.DefaultExt = ".stl";
            dlg.Filter = "STL mesh (.stl)|*.stl";
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                string filename = dlg.FileName;
                GenerateSignage().WriteMesh(filename);
            }
        }
    }
}

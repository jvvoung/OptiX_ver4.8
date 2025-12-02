using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OptiX.Common;
using OptiX.DLL;

namespace OptiX.Common
{
    /// <summary>
    /// CellIdInputWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CellIdInputWindow : Window
    {
        private int zoneNumber;
        private bool isDarkMode;
        private string iniSection;
        private bool isCimEnabled = false;
        private bool isEecpEnabled = false;
        private bool isEecpSummaryEnabled = false;
        private bool isValidationEnabled = false;

        private class PortBinding
        {
            public string IniSection { get; set; } = "MTP";
            public List<string> IniKeys { get; } = new List<string>();
            public TextBox TextBox { get; set; }
            public string PortType { get; set; } = "MEAS";
            public string DisplayName { get; set; } = "";
            public Button ConnectButton { get; set; }

            public string StateKey
            {
                get
                {
                    string keyPart = IniKeys != null && IniKeys.Count > 0
                        ? string.Join("|", IniKeys)
                        : string.Empty;
                    return $"{PortType}:{IniSection}:{keyPart}";
                }
            }
        }

        private class ConnectContext
        {
            public string ContextType { get; set; } = "";
            public List<int> Zones { get; set; } = new List<int>();
            public string PortLabel { get; set; } = "";
            public string SectionName { get; set; } = "";
            public List<string> IniKeys { get; set; } = new List<string>();
            public List<PortBinding> Bindings { get; set; } = new List<PortBinding>();
        }

        private class CellInfoBinding
        {
            public List<int> Zones { get; } = new List<int>();
            public TextBox CellIdTextBox { get; set; }
            public TextBox InnerIdTextBox { get; set; }
            public string DisplayName { get; set; } = "";
        }

        private readonly List<CellInfoBinding> cellBindings = new List<CellInfoBinding>();
        private readonly List<PortBinding> portBindings = new List<PortBinding>();
        private static readonly Dictionary<string, bool> connectionStates = new Dictionary<string, bool>();
        private bool isHviModeEnabled = false;
        private bool isMeasMultiEnabled = false;
        private int totalZoneCount = 1;
        private string[] wadAngles = Array.Empty<string>();

        public CellIdInputWindow() : this(1)
        {
        }

        public CellIdInputWindow(int zoneNumber) : this(zoneNumber, false)
        {
        }

        public CellIdInputWindow(int zoneNumber, bool isDarkMode) : this(zoneNumber, isDarkMode, "MTP")
        {
        }

        public CellIdInputWindow(int zoneNumber, bool isDarkMode, string iniSection)
        {
            InitializeComponent();
            this.zoneNumber = zoneNumber;
            this.isDarkMode = isDarkMode;
            this.iniSection = iniSection;
            InitializeConfigurationOptions();
            
            // INI 섹션에 따라 창 제목 설정
            string inspectionType = iniSection == "IPVS" ? "IPVS" : "OPTIC";
            this.Title = $"Zone {zoneNumber} {inspectionType} SETTING";
            
            // 다크모드 적용
            ApplyTheme();
            
            // 언어 적용
            ApplyLanguage();
            
            // 추가 초기화는 UI가 로드된 후 실행
            this.Loaded += (s, e) => {
                try 
                {
                   BuildCellInfoSections();
                   LoadCellInformation();
                   BuildPortSections();
                   LoadPortSettings();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"추가 초기화 오류: {ex.Message}");
                }
            };
        }

        private void InitializeConfigurationOptions()
        {
            bool isOptic = iniSection != "IPVS";
            totalZoneCount = isOptic
                ? int.Parse(GlobalDataManager.GetValue("Settings", "MTP_ZONE", "2"))
                : int.Parse(GlobalDataManager.GetValue("Settings", "IPVS_ZONE", "2"));

            string measMultiRaw = GlobalDataManager.GetValue("Settings", "MEAS_MULTI",
                GlobalDataManager.GetValue("Settings", "MEAS_MULIT", "F"));
            isMeasMultiEnabled = measMultiRaw.Trim().Equals("T", StringComparison.OrdinalIgnoreCase);
            isHviModeEnabled = isOptic && GlobalDataManager.IsHviModeEnabled();

            string wadSection = isOptic ? "MTP" : "IPVS";
            string wadDefault = isOptic ? "0,15,30,45,60" : "0,30,60,90,120";
            string wadValue = GlobalDataManager.GetValue(wadSection, "WAD", wadDefault);
            wadAngles = wadValue
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim())
                .Where(w => !string.IsNullOrEmpty(w))
                .ToArray();

            if (wadAngles.Length == 0)
            {
                wadAngles = new[] { "0" };
            }
        }

        private void BuildCellInfoSections()
        {
            if (CellInfoStackPanel == null)
            {
                return;
            }

            CellInfoStackPanel.Children.Clear();
            cellBindings.Clear();

            if (isHviModeEnabled)
            {
                CreateCellInfoSection("Cell", Enumerable.Range(1, totalZoneCount).ToList());
            }
            else
            {
                for (int zone = 1; zone <= totalZoneCount; zone++)
                {
                    CreateCellInfoSection($"Zone {zone}", new List<int> { zone });
                }
            }
        }

        private void CreateCellInfoSection(string header, List<int> zones)
        {
            var border = new Border
            {
                Background = (Brush)FindResource("DynamicCardBackground"),
                BorderBrush = (Brush)FindResource("DynamicBorderColor"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var headerText = new TextBlock
            {
                Text = header,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("DynamicTextColor"),
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(headerText, 0);

            var cellIdGrid = CreateCellInfoRow("Cell ID", out TextBox cellIdTextBox);
            Grid.SetRow(cellIdGrid, 1);

            var innerIdGrid = CreateCellInfoRow("Inner ID", out TextBox innerIdTextBox);
            Grid.SetRow(innerIdGrid, 2);

            grid.Children.Add(headerText);
            grid.Children.Add(cellIdGrid);
            grid.Children.Add(innerIdGrid);

            border.Child = grid;
            CellInfoStackPanel.Children.Add(border);

            var binding = new CellInfoBinding
            {
                CellIdTextBox = cellIdTextBox,
                InnerIdTextBox = innerIdTextBox,
                DisplayName = header
            };
            binding.Zones.AddRange(zones);

            cellBindings.Add(binding);
        }

        private Grid CreateCellInfoRow(string labelText, out TextBox textBox)
        {
            var rowGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var label = new TextBlock
            {
                Text = labelText,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)FindResource("DynamicTextColor"),
                FontSize = 13
            };
            Grid.SetColumn(label, 0);

            textBox = new TextBox();
            try
            {
                textBox.Style = (Style)FindResource("PortTextBoxStyle");
            }
            catch
            {
                textBox.Height = 32;
                textBox.FontSize = 12;
                textBox.Padding = new Thickness(8, 4, 8, 4);
            }
            Grid.SetColumn(textBox, 1);

        rowGrid.Children.Add(label);
        rowGrid.Children.Add(textBox);

        return rowGrid;
        }

        private void LoadCellInformation()
        {
            string section = iniSection == "IPVS" ? "IPVS" : "MTP";

            foreach (var binding in cellBindings)
            {
                if (binding.CellIdTextBox == null || binding.InnerIdTextBox == null)
                {
                    continue;
                }

                string cellIdValue = "";
                string innerIdValue = "";

                if (binding.Zones.Count > 0)
                {
                    int primaryZone = binding.Zones[0];
                    cellIdValue = GlobalDataManager.GetValue(section, $"CELL_ID_ZONE_{primaryZone}", "");
                    innerIdValue = GlobalDataManager.GetValue(section, $"INNER_ID_ZONE_{primaryZone}", "");

                    bool allSame = true;
                    foreach (int zone in binding.Zones.Skip(1))
                    {
                        string otherCell = GlobalDataManager.GetValue(section, $"CELL_ID_ZONE_{zone}", "");
                        string otherInner = GlobalDataManager.GetValue(section, $"INNER_ID_ZONE_{zone}", "");

                        if (!string.Equals(cellIdValue, otherCell, StringComparison.OrdinalIgnoreCase) ||
                            !string.Equals(innerIdValue, otherInner, StringComparison.OrdinalIgnoreCase))
                        {
                            allSame = false;
                            break;
                        }
                    }

                    if (!allSame)
                    {
                        cellIdValue = "";
                        innerIdValue = "";
                    }
                }

                binding.CellIdTextBox.Text = cellIdValue ?? "";
                binding.InnerIdTextBox.Text = innerIdValue ?? "";
            }
        }

        private void BuildPortSections()
        {
            if (PortSectionsStackPanel == null) return;

            PortSectionsStackPanel.Children.Clear();
            portBindings.Clear();

            BuildPgSections();
            BuildMeasSections();
        }

        private void BuildPgSections()
        {
            if (totalZoneCount <= 0)
            {
                totalZoneCount = 1;
            }

            if (isHviModeEnabled)
            {
                CreatePgSection("PG", Enumerable.Range(1, totalZoneCount).ToList(), suppressSectionButton: true);
            }
            else
            {
                for (int zone = 1; zone <= totalZoneCount; zone++)
                {
                    CreatePgSection($"PG_{zone}", new List<int> { zone }, suppressSectionButton: true);
                }
            }
        }

        private void CreatePgSection(string header, List<int> zoneIndices, bool suppressSectionButton)
        {
            var context = new ConnectContext
            {
                ContextType = "PG_SECTION",
                Zones = zoneIndices.ToList(),
                PortLabel = header,
                SectionName = header,
                Bindings = new List<PortBinding>()
            };

            var sectionContent = CreateSectionContainer(header, $"{header} CONNECT", SectionConnectButton_Click, context, suppressSectionButton);

            var portContext = new ConnectContext
            {
                ContextType = "PG_PORT",
                Zones = zoneIndices.ToList(),
                PortLabel = "Port",
                SectionName = header,
                Bindings = new List<PortBinding>()
            };

            var portRow = CreatePortRow("Port", "CONNECT", PortConnectButton_Click, portContext);
            sectionContent.Children.Add(portRow.row);

            var binding = new PortBinding
            {
                IniSection = iniSection,
                TextBox = portRow.textBox,
                PortType = "PG",
                DisplayName = $"{header} ({FormatZoneList(zoneIndices)})",
                ConnectButton = portRow.button
            };

            foreach (int zone in zoneIndices)
            {
                binding.IniKeys.Add($"PG_PORT_{zone}");
            }

            portBindings.Add(binding);
            ApplyStoredConnectionState(binding);
            context.Bindings.Add(binding);
            portContext.Bindings.Add(binding);
        }

        private void BuildMeasSections()
        {
            string measSection = iniSection == "IPVS" ? "IPVS" : "MTP";

            for (int zone = 1; zone <= totalZoneCount; zone++)
            {
                string header = $"MEAS_{zone}";
                var groupContext = new ConnectContext
                {
                    ContextType = "MEAS_SECTION",
                    Zones = new List<int> { zone },
                    PortLabel = header,
                    SectionName = header,
                    Bindings = new List<PortBinding>()
                };

                var sectionContent = CreateSectionContainer(header, $"{header} CONNECT", SectionConnectButton_Click, groupContext, suppressSectionButton: isMeasMultiEnabled);

                if (isMeasMultiEnabled)
                {
                    var portContext = new ConnectContext
                    {
                        ContextType = "MEAS_PORT",
                        Zones = new List<int> { zone },
                        PortLabel = "Port",
                        SectionName = header,
                        Bindings = new List<PortBinding>(),
                        IniKeys = new List<string> { $"MEAS_PORT_{zone}" }
                    };

                    var portRow = CreatePortRow("Port", "CONNECT", PortConnectButton_Click, portContext);
                    sectionContent.Children.Add(portRow.row);

                    var binding = new PortBinding
                    {
                        IniSection = measSection,
                        TextBox = portRow.textBox,
                        PortType = "MEAS",
                        DisplayName = $"{header} ({FormatZoneList(new List<int> { zone })})",
                        ConnectButton = portRow.button
                    };
                    binding.IniKeys.Add($"MEAS_PORT_{zone}");
                    portBindings.Add(binding);
                    ApplyStoredConnectionState(binding);
                    portContext.Bindings.Add(binding);
                    groupContext.Bindings.Add(binding);
                    groupContext.Bindings.Add(binding);
                }
                else
                {
                    bool basePortCreated = false;
                    var zoneBindings = new List<PortBinding>();

                    foreach (string wad in wadAngles)
                    {
                        string wadTrimmed = wad.Trim();
                        if (string.IsNullOrEmpty(wadTrimmed))
                            continue;

                        string label = wadTrimmed == "0" ? "Port" : $"Port_{wadTrimmed}";
                        string key = wadTrimmed == "0"
                            ? $"MEAS_PORT_{zone}"
                            : $"MEAS_PORT_{wadTrimmed}_{zone}";

                        if (wadTrimmed == "0")
                        {
                            basePortCreated = true;
                        }

                        var portContext = new ConnectContext
                        {
                            ContextType = "MEAS_PORT",
                            Zones = new List<int> { zone },
                            PortLabel = label,
                            SectionName = header,
                            Bindings = new List<PortBinding>(),
                            IniKeys = new List<string> { key }
                        };

                        var portRow = CreatePortRow(label, "CONNECT", PortConnectButton_Click, portContext);
                        sectionContent.Children.Add(portRow.row);

                        var binding = new PortBinding
                        {
                            IniSection = measSection,
                            TextBox = portRow.textBox,
                            PortType = "MEAS",
                            DisplayName = $"{header} - {label} ({FormatZoneList(new List<int> { zone })})",
                            ConnectButton = portRow.button
                        };
                        binding.IniKeys.Add(key);
                        portBindings.Add(binding);
                        ApplyStoredConnectionState(binding);
                        portContext.Bindings.Add(binding);
                        zoneBindings.Add(binding);
                    }

                    if (!basePortCreated)
                    {
                        string key = $"MEAS_PORT_{zone}";
                        var portContext = new ConnectContext
                        {
                            ContextType = "MEAS_PORT",
                            Zones = new List<int> { zone },
                            PortLabel = "Port",
                            SectionName = header,
                            Bindings = new List<PortBinding>(),
                            IniKeys = new List<string> { key }
                        };

                        var portRow = CreatePortRow("Port", "CONNECT", PortConnectButton_Click, portContext);
                        sectionContent.Children.Add(portRow.row);

                        var binding = new PortBinding
                        {
                            IniSection = measSection,
                            TextBox = portRow.textBox,
                            PortType = "MEAS",
                            DisplayName = $"{header} - Port ({FormatZoneList(new List<int> { zone })})",
                            ConnectButton = portRow.button
                        };
                        binding.IniKeys.Add(key);
                        portBindings.Add(binding);
                        ApplyStoredConnectionState(binding);
                        portContext.Bindings.Add(binding);
                        zoneBindings.Add(binding);
                    }

                    groupContext.Bindings = zoneBindings;
                }
            }
        }

        private StackPanel CreateSectionContainer(string header, string connectButtonText, RoutedEventHandler connectHandler, ConnectContext context)
            => CreateSectionContainer(header, connectButtonText, connectHandler, context, suppressSectionButton: false);

        private StackPanel CreateSectionContainer(string header, string connectButtonText, RoutedEventHandler connectHandler, ConnectContext context, bool suppressSectionButton)
        {
            var border = new Border
            {
                Background = (Brush)FindResource("DynamicCardBackground"),
                BorderBrush = (Brush)FindResource("DynamicBorderColor"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var headerText = new TextBlock
            {
                Text = header,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("DynamicTextColor"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(headerText, 0);

            var connectButton = new Button
            {
                Content = connectButtonText,
                Style = (Style)FindResource("ModernButtonStyle"),
                FontSize = 11,
                Height = 28,
                Padding = new Thickness(10, 2, 10, 2),
                Tag = context
            };
            connectButton.Click += connectHandler;
            Grid.SetColumn(connectButton, 1);

            headerGrid.Children.Add(headerText);
            if (!suppressSectionButton)
            {
                headerGrid.Children.Add(connectButton);
            }

            var contentStack = new StackPanel { Orientation = Orientation.Vertical };

            Grid.SetRow(headerGrid, 0);
            Grid.SetRow(contentStack, 1);

            grid.Children.Add(headerGrid);
            grid.Children.Add(contentStack);

            border.Child = grid;
            PortSectionsStackPanel.Children.Add(border);

            return contentStack;
        }

        private (Grid row, TextBox textBox, Button button) CreatePortRow(string label, string buttonText, RoutedEventHandler clickHandler, ConnectContext context)
        {
            var rowGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var labelBlock = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeights.Bold,
                Background = (Brush)FindResource("PrimaryColor"),
                Foreground = Brushes.White,
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 70,
                TextAlignment = TextAlignment.Center
            };
            Grid.SetColumn(labelBlock, 0);

            var textBox = new TextBox
            {
                Margin = new Thickness(0, 0, 8, 0)
            };
            try
            {
                textBox.Style = (Style)FindResource("PortTextBoxStyle");
            }
            catch
            {
                textBox.Height = 32;
                textBox.FontSize = 12;
                textBox.Padding = new Thickness(8, 4, 8, 4);
            }
            Grid.SetColumn(textBox, 1);

            var button = new Button
            {
                Content = buttonText,
                Style = (Style)FindResource("ModernButtonStyle"),
                FontSize = 11,
                Height = 28,
                Padding = new Thickness(10, 2, 10, 2),
                Tag = context
            };
            button.Click += clickHandler;
            Grid.SetColumn(button, 2);

            rowGrid.Children.Add(labelBlock);
            rowGrid.Children.Add(textBox);
            rowGrid.Children.Add(button);

            return (rowGrid, textBox, button);
        }

        private void ApplyStoredConnectionState(PortBinding binding)
        {
            if (binding == null || binding.ConnectButton == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(binding.StateKey) && connectionStates.TryGetValue(binding.StateKey, out bool state))
            {
                ApplyButtonResult(binding.ConnectButton, state);
            }
            else
            {
                ResetButtonAppearance(binding.ConnectButton);
            }
        }

        private void ApplyTheme()
        {
            if (isDarkMode)
            {
                // 다크모드 색상 적용
                this.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42));
                
                // 동적 리소스 업데이트 - 다크모드
                Resources["DynamicWindowBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)); // #0F172A
                Resources["DynamicCardBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59)); // #1E293B
                Resources["DynamicBorderColor"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105)); // #475569
                Resources["DynamicTextColor"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249)); // #F1F5F9
                Resources["DynamicInputBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85)); // #334155
                Resources["DynamicInputBorder"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105)); // #475569
                Resources["DynamicSecondaryButtonBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85)); // #334155
                Resources["DynamicSecondaryButtonText"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225)); // #CBD5E1
            }
            else
            {
                // 라이트모드 색상 적용 (기본값)
                this.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252));
                
                // 동적 리소스 업데이트 - 라이트모드
                Resources["DynamicWindowBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)); // #F8FAFC
                Resources["DynamicCardBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)); // #FFFFFF
                Resources["DynamicBorderColor"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)); // #E2E8F0
                Resources["DynamicTextColor"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59)); // #1E293B
                Resources["DynamicInputBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)); // #FFFFFF
                Resources["DynamicInputBorder"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(209, 213, 219)); // #D1D5DB
                Resources["DynamicSecondaryButtonBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 244, 246)); // #F3F4F6
                Resources["DynamicSecondaryButtonText"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(55, 65, 81)); // #374151
            }
        }

        private void LoadPortSettings()
        {
            try
            {
                foreach (var binding in portBindings)
                {
                    string value = GetIniValue(binding.IniSection, binding.IniKeys);

                    if (binding.TextBox != null)
                    {
                        binding.TextBox.Text = value ?? "";
                    }
                }

                System.Diagnostics.Debug.WriteLine($"포트 설정 로드 완료: {portBindings.Count}개의 바인딩");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"포트 설정 로드 오류: {ex.Message}");
            }
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Cell 정보 저장
                SaveCellInformation();
                
                // 파일 생성 여부 저장 (현재는 메모리 변수로만 저장)
                SaveFileGenerationSettings();
                
                // 포트 설정 저장
                SavePortSettings();

                // INI 파일 저장 후 전역 캐시 갱신
                GlobalDataManager.Reload();

                MessageBox.Show("모든 설정이 성공적으로 저장되었습니다.", "저장 완료", 
                              MessageBoxButton.OK, MessageBoxImage.Information);

                // Non-Modal 창에서는 DialogResult 사용하지 않음
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 중 오류가 발생했습니다: {ex.Message}", "오류", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveCellInformation()
        {
            string section = iniSection == "IPVS" ? "IPVS" : "MTP";

            foreach (var binding in cellBindings)
            {
                string cellId = binding.CellIdTextBox?.Text?.Trim() ?? "";
                string innerId = binding.InnerIdTextBox?.Text?.Trim() ?? "";

                if (binding.Zones.Count == 0)
                {
                    continue;
                }

                foreach (int zone in binding.Zones)
                {
                    GlobalDataManager.SetValue(section, $"CELL_ID_ZONE_{zone}", cellId);
                    GlobalDataManager.SetValue(section, $"INNER_ID_ZONE_{zone}", innerId);
                }

                System.Diagnostics.Debug.WriteLine($"Cell 정보 저장: {binding.DisplayName} => CELL_ID='{cellId}', INNER_ID='{innerId}' (Zones: {FormatZoneList(binding.Zones)})");
            }
        }

        private void SaveFileGenerationSettings()
        {
            try
            {
                // T/F 형태로 INI 파일에 저장
                string cimValue = isCimEnabled ? "T" : "F";
                string eecpValue = isEecpEnabled ? "T" : "F";
                string eecpSummaryValue = isEecpSummaryEnabled ? "T" : "F";
                string validationValue = isValidationEnabled ? "T" : "F";
                
                GlobalDataManager.SetValue(iniSection, "CREATE_CIM", cimValue);
                GlobalDataManager.SetValue(iniSection, "CREATE_EECP", eecpValue);
                GlobalDataManager.SetValue(iniSection, "CREATE_EECP_SUMMARY", eecpSummaryValue);
                GlobalDataManager.SetValue(iniSection, "CREATE_VALIDATION", validationValue);
                
                System.Diagnostics.Debug.WriteLine($"파일 생성 설정 저장 완료 - CIM: {cimValue}, EECP: {eecpValue}, EECP_SUMMARY: {eecpSummaryValue}, VALIDATION: {validationValue}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"파일 생성 설정 저장 오류: {ex.Message}");
                throw; // 저장 실패 시 상위로 예외 전달
            }
        }


        private void SavePortSettings()
        {
            try
            {
                foreach (var binding in portBindings)
                {
                    string text = binding.TextBox?.Text?.Trim() ?? "";
                    
                    foreach (var key in binding.IniKeys)
                    {
                        GlobalDataManager.SetValue(binding.IniSection, key, text);
                    }
                    }
                    
                if (iniSection == "IPVS")
                {
                    IPVSDataManager.LoadFromIni();
                }
                else
                {
                    MTPDataManager.LoadFromIni();
                }

                System.Diagnostics.Debug.WriteLine($"포트 설정 저장 완료 - 바인딩 수: {portBindings.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"포트 설정 저장 오류: {ex.Message}");
                throw;
            }
        }

        private string GetIniValue(string section, List<string> keys)
        {
            if (keys == null || keys.Count == 0)
            {
                return "";
            }

            var sectionData = GlobalDataManager.ReadSection(section);

            foreach (string key in keys)
            {
                if (sectionData != null && sectionData.TryGetValue(key, out string rawValue))
                {
                    if (!string.IsNullOrWhiteSpace(rawValue))
                    {
                        return rawValue.Trim();
                    }
                }

                string cachedValue = GlobalDataManager.GetValue(section, key, "");
                if (!string.IsNullOrWhiteSpace(cachedValue))
                {
                    return cachedValue.Trim();
                }
            }

            return "";
        }

        private void AllConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (portBindings.Count == 0)
                {
                    MessageBox.Show("연결할 포트가 없습니다.", "ALL CONNECT",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _ = ExecutePortBindings(
                    portBindings,
                    "ALL CONNECT",
                    showSummary: true,
                    sourceButton: sender as Button);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"전체 연결 중 오류가 발생했습니다: {ex.Message}", "연결 오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SectionConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is ConnectContext context)
                {
                    if (context.Bindings == null || context.Bindings.Count == 0)
                    {
                        MessageBox.Show("연결할 포트가 없습니다.", "Section Connect",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    _ = ExecutePortBindings(
                        context.Bindings,
                        string.IsNullOrWhiteSpace(context.SectionName) ? context.PortLabel : $"{context.SectionName} CONNECT",
                        showSummary: true,
                        sourceButton: button);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"섹션 연결 중 오류가 발생했습니다: {ex.Message}", "연결 오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PortConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is ConnectContext context)
                {
                    if (context.Bindings == null || context.Bindings.Count == 0)
                    {
                        MessageBox.Show("연결할 포트가 없습니다.", "Port Connect",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    _ = ExecutePortBindings(
                        context.Bindings,
                        string.IsNullOrWhiteSpace(context.SectionName)
                            ? $"{context.PortLabel} CONNECT"
                            : $"{context.SectionName} - {context.PortLabel}",
                        showSummary: false,
                        sourceButton: button);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"포트 연결 중 오류가 발생했습니다: {ex.Message}", "연결 오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ExecutePortBindings(IEnumerable<PortBinding> bindings, string contextTitle, bool showSummary, Button sourceButton)
        {
            var successList = new List<string>();
            var failureList = new List<string>();

            foreach (var binding in bindings)
            {
                string displayName = binding.DisplayName;
                string portText = binding.TextBox?.Text?.Trim() ?? "";

                bool bindingSuccess = false;

                if (string.IsNullOrEmpty(portText))
                {
                    failureList.Add($"{displayName}: 포트 번호가 비어 있습니다.");
                    ApplyButtonResult(binding.ConnectButton, false);
                    continue;
                }

                if (!int.TryParse(portText, out int portNumber))
                {
                    failureList.Add($"{displayName}: 숫자로 변환할 수 없습니다. (입력값: {portText})");
                    ApplyButtonResult(binding.ConnectButton, false);
                    continue;
                }

                bool result;
                try
                {
                    result = binding.PortType == "PG"
                        ? DllFunctions.CallPGTurn(portNumber)
                        : DllFunctions.CallMeasTurn(portNumber);
                }
                catch (Exception ex)
                {
                    failureList.Add($"{displayName}: 예외 발생 - {ex.Message}");
                    continue;
                }

                if (result)
                {
                    successList.Add($"{displayName}: 성공 (Port {portNumber})");
                    bindingSuccess = true;
                }
                else
                {
                    failureList.Add($"{displayName}: 실패 (Port {portNumber})");
                }

                ApplyButtonResult(binding.ConnectButton, bindingSuccess);
                UpdateConnectionState(binding, bindingSuccess);
            }

            bool allSuccess = failureList.Count == 0;
            ApplyButtonResult(sourceButton, allSuccess);

            if (showSummary)
            {
                var sb = new StringBuilder();
                if (successList.Count > 0)
                {
                    sb.AppendLine("[성공]");
                    foreach (var msg in successList)
                        sb.AppendLine($" - {msg}");
                }
                if (failureList.Count > 0)
                {
                    if (sb.Length > 0) sb.AppendLine();
                    sb.AppendLine("[실패]");
                    foreach (var msg in failureList)
                        sb.AppendLine($" - {msg}");
                }

                string message = sb.Length > 0 ? sb.ToString() : "실행할 포트가 없습니다.";
                MessageBox.Show(message, contextTitle,
                    MessageBoxButton.OK,
                    failureList.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            }
            else
            {
                string message;
                MessageBoxImage icon;

                if (failureList.Count > 0)
                {
                    message = failureList.First();
                    icon = MessageBoxImage.Warning;
                }
                else
                {
                    message = successList.FirstOrDefault() ?? "성공적으로 수행했습니다.";
                    icon = MessageBoxImage.Information;
                }

                MessageBox.Show(message, contextTitle, MessageBoxButton.OK, icon);
            }

            return allSuccess;
        }

        private void ApplyButtonResult(Button button, bool success)
        {
            if (button == null)
            {
                return;
            }

            ResetButtonAppearance(button);

            var successBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // #22C55E
            var failureBrush = new SolidColorBrush(Color.FromRgb(248, 113, 113)); // #F87171
            var targetBrush = success ? successBrush : failureBrush;

            button.Background = targetBrush;
            button.BorderBrush = targetBrush;
            button.Foreground = Brushes.White;
        }

        private void ResetButtonAppearance(Button button)
        {
            if (button == null)
            {
                return;
            }

            var baseStyle = FindResource("ModernButtonStyle") as Style;
            if (baseStyle != null)
            {
                button.Style = baseStyle;
            }
            button.ClearValue(Button.BackgroundProperty);
            button.ClearValue(Button.BorderBrushProperty);
            button.ClearValue(Button.ForegroundProperty);
        }

        private void UpdateConnectionState(PortBinding binding, bool success)
        {
            if (binding == null || string.IsNullOrEmpty(binding.StateKey))
            {
                return;
            }

            connectionStates[binding.StateKey] = success;
        }

        private static string FormatZoneList(IEnumerable<int> zones)
        {
            if (zones == null)
            {
                return "Zone ?";
            }

            var list = zones.ToList();
            if (list.Count == 0)
            {
                return "Zone ?";
            }

            return string.Join(", ", list.Select(z => $"Zone {z}"));
        }

        // 언어 적용 메서드
        public void ApplyLanguage()
        {
            try
            {
                // 창 제목을 INI 섹션에 따라 동적으로 설정
                string inspectionType = iniSection == "IPVS" ? "IPVS" : "OPTIC";
                this.Title = $"Zone {zoneNumber} {inspectionType} SETTING";
                
                // Cell 정보 제목
                if (CellInfoTitle != null)
                    CellInfoTitle.Text = LanguageManager.GetText("CellIdInput.CellInfo");
                
                
                // Port 연결 제목
                if (PortConnectionTitle != null)
                    PortConnectionTitle.Text = LanguageManager.GetText("CellIdInput.PortConnection");
                
                // 취소 버튼
                if (CancelButton != null)
                    CancelButton.Content = LanguageManager.GetText("CellIdInput.Cancel");
                
                System.Diagnostics.Debug.WriteLine($"CellIdInputWindow 언어 적용 완료: {LanguageManager.CurrentLanguage}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CellIdInputWindow 언어 적용 오류: {ex.Message}");
            }
        }

        // 누락된 이벤트 핸들러 추가
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Non-Modal 창에서는 DialogResult 사용하지 않음
            this.Close();
        }
    }
}


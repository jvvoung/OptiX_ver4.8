using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace OptiX
{
    /// <summary>
    /// LUTPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LUTPage : UserControl
    {
        private bool _isDarkMode = false;
        // 마지막으로 그린 LUT 파라미터를 보관하여 리사이즈 시 재사용
        private LUTParameter? _lastRed;
        private LUTParameter? _lastGreen;
        private LUTParameter? _lastBlue;


        public LUTPage()
        {
            InitializeComponent();
            LoadThemeFromIni();
            // 언어 변경 이벤트 구독 및 초기 적용
            LanguageManager.LanguageChanged += (_, __) => UpdateLocalizedTexts();
            UpdateLocalizedTexts();
        }


        /// <summary>
        /// INI 파일에서 테마 설정 로드 (MainWindow와 동일한 방식)
        /// </summary>
        private void LoadThemeFromIni()
        {
            try
            {
                // MainWindow와 동일한 방식으로 테마 로드
                string iniPath = @"D:\\Project\\Recipe\\OptiX.ini";
                var iniManager = new IniFileManager(iniPath);
                string isDarkModeStr = iniManager.ReadValue("Theme", "IsDarkMode", "False");
                _isDarkMode = bool.TryParse(isDarkModeStr, out bool darkMode) && darkMode;
                ApplyTheme();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"테마 로드 오류: {ex.Message}");
                _isDarkMode = false; // 기본값은 라이트 모드
                ApplyTheme();
            }
        }

        private void ApplyTheme()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"LUT 페이지 ApplyTheme 시작: {(_isDarkMode ? "다크" : "라이트")} 모드");
                ThemeManager.UpdateDynamicColors(this, _isDarkMode);
                System.Diagnostics.Debug.WriteLine($"LUT 페이지 ThemeManager.UpdateDynamicColors 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"테마 적용 오류: {ex.Message}");
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("LUT Reset 버튼 클릭됨");
                
                // 모든 그래프 영역 초기화
                ClearGraph("RedGraphCanvas");
                ClearGraph("GreenGraphCanvas");
                ClearGraph("BlueGraphCanvas");
                
                // 모든 공식 영역 초기화
                ClearFormula("RedFormulaText");
                ClearFormula("GreenFormulaText");
                ClearFormula("BlueFormulaText");
                
                // 모든 파라미터 영역 초기화
                ClearParameter("RedParameterText");
                ClearParameter("GreenParameterText");
                ClearParameter("BlueParameterText");
                
                // Total Parameters 초기화
                var totalParamText = (TextBox)this.FindName("TotalParameterTextBox");
                if (totalParamText != null)
                {
                    totalParamText.Text = "";
                    totalParamText.FontSize = 12; // 원래 크기로 복원
                }
                
                // 컨트롤 패널 입력값 초기화
                ClearTextBox("RVTextBox");
                ClearTextBox("GVTextBox");
                ClearTextBox("BVTextBox");
                ClearTextBox("IntervalTextBox");
                ClearTextBox("CountTextBox");
                
                System.Diagnostics.Debug.WriteLine("Reset 완료");
                MessageBox.Show("🔄 모든 데이터가 초기화되었습니다.", "Reset 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Reset 오류: {ex.Message}");
                MessageBox.Show($"Reset 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ClearGraph(string canvasName)
        {
            var canvas = (Canvas)this.FindName(canvasName);
            if (canvas != null)
            {
                canvas.Children.Clear();
            }
            // 그래프 플레이스홀더는 남겨둠
            var placeholder = (TextBlock)this.FindName(canvasName.Replace("Canvas", "GraphPlaceholder"));
            if (placeholder != null) placeholder.Visibility = Visibility.Visible;
        }
        
        private void ClearFormula(string textBlockName)
        {
            var textBlock = (TextBlock)this.FindName(textBlockName);
            if (textBlock != null)
            {
                textBlock.Text = "공식";
                textBlock.Inlines.Clear();
            }
        }
        
        private void ClearParameter(string textBlockName)
        {
            var textBlock = (TextBlock)this.FindName(textBlockName);
            if (textBlock != null)
            {
                string colorName = textBlockName.Contains("Red") ? "RED" : (textBlockName.Contains("Green") ? "GREEN" : "BLUE");
                textBlock.Text = $"{colorName} 텍스트 박스 파라미터 값";
            }
        }
        
        private void ClearTextBox(string textBoxName)
        {
            var textBox = (TextBox)this.FindName(textBoxName);
            if (textBox != null)
            {
                textBox.Text = "";
            }
        }

        private async void TestStartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("LUT Test Start 버튼 클릭됨");
                
                // 컨트롤 패널에서 값 읽기
                float rv = GetFloatFromTextBox("RVTextBox");
                float gv = GetFloatFromTextBox("GVTextBox");
                float bv = GetFloatFromTextBox("BVTextBox");
                int interval = GetIntFromTextBox("IntervalTextBox");
                int count = GetIntFromTextBox("CountTextBox");
                
                // LUT 파라미터 배열 생성
                var lutParams = new LUTParameter[3];
                
                // RGB 각각에 대해 함수 호출 (0=Red, 1=Green, 2=Blue)
                for (int rgb = 0; rgb < 3; rgb++)
                {
                    System.Diagnostics.Debug.WriteLine($"RGB {rgb} 처리 시작...");
                    
                    var (lutParam, success) = DllManager.CallGetLUTdata(rgb, rv, gv, bv, interval, count);
                    
                    if (success)
                    {
                        lutParams[rgb] = lutParam;
                        // 결과를 UI에 표시
                        UpdateLUTDisplay(rgb, lutParam);
                        // 마지막 파라미터 저장
                        if (rgb == 0) _lastRed = lutParam;
                        else if (rgb == 1) _lastGreen = lutParam;
                        else _lastBlue = lutParam;
                        System.Diagnostics.Debug.WriteLine($"RGB {rgb} 처리 완료");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"RGB {rgb} 처리 실패");
                    }
                    
                    // 1초 대기 (마지막 RGB는 대기하지 않음)
                    if (rgb < 2)
                    {
                        await Task.Delay(1000);
                    }
                }
                
                // Total Parameters 업데이트
                UpdateTotalParameters(lutParams);
                
                System.Diagnostics.Debug.WriteLine("LUT Test 완료");
                
                // 테스트 완료 팝업 표시
                MessageBox.Show("🎉 LUT 테스트가 완료되었습니다!", 
                              "테스트 완료", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Test Start 오류: {ex.Message}");
                MessageBox.Show($"Test Start 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private float GetFloatFromTextBox(string textBoxName)
        {
            var textBox = (TextBox)this.FindName(textBoxName);
            if (textBox != null && float.TryParse(textBox.Text, out float value))
            {
                return value;
            }
            return 0.0f;
        }
        
        private int GetIntFromTextBox(string textBoxName)
        {
            var textBox = (TextBox)this.FindName(textBoxName);
            if (textBox != null && int.TryParse(textBox.Text, out int value))
            {
                return value;
            }
            return 0;
        }
        
        private void UpdateLUTDisplay(int rgbIndex, LUTParameter lutParam)
        {
            string colorName = rgbIndex == 0 ? "Red" : (rgbIndex == 1 ? "Green" : "Blue");
            
            // 그래프 영역 업데이트 (XY 그래프 그리기)
            DrawLUTGraph(rgbIndex, lutParam);
            // 해당 그래프 플레이스홀더 숨김
            var placeholder = (TextBlock)this.FindName((rgbIndex==0?"Red":rgbIndex==1?"Green":"Blue")+"GraphPlaceholder");
            if (placeholder != null) placeholder.Visibility = Visibility.Collapsed;
            
            // 공식 영역 업데이트
            UpdateFormulaArea(rgbIndex, lutParam);
            
            // 파라미터 값 영역 업데이트
            UpdateParameterArea(rgbIndex, lutParam);
        }

        private void UpdateLocalizedTexts()
        {
            // 그래프 플레이스홀더
            var redGraphPh = (TextBlock)this.FindName("RedGraphPlaceholder");
            if (redGraphPh != null) redGraphPh.Text = LanguageManager.GetText("LUTPage.Red.Graph");
            var greenGraphPh = (TextBlock)this.FindName("GreenGraphPlaceholder");
            if (greenGraphPh != null) greenGraphPh.Text = LanguageManager.GetText("LUTPage.Green.Graph");
            var blueGraphPh = (TextBlock)this.FindName("BlueGraphPlaceholder");
            if (blueGraphPh != null) blueGraphPh.Text = LanguageManager.GetText("LUTPage.Blue.Graph");

            // 공식 영역 - 항상 언어에 맞게 업데이트
            var redFormula = (TextBlock)this.FindName("RedFormulaText");
            if (redFormula != null && redFormula.Inlines.Count == 0) redFormula.Text = LanguageManager.GetText("LUTPage.Red.Formula");
            var greenFormula = (TextBlock)this.FindName("GreenFormulaText");
            if (greenFormula != null && greenFormula.Inlines.Count == 0) greenFormula.Text = LanguageManager.GetText("LUTPage.Green.Formula");
            var blueFormula = (TextBlock)this.FindName("BlueFormulaText");
            if (blueFormula != null && blueFormula.Inlines.Count == 0) blueFormula.Text = LanguageManager.GetText("LUTPage.Blue.Formula");

            // 파라미터 영역 - 항상 언어에 맞게 업데이트
            var redParam = (TextBlock)this.FindName("RedParameterText");
            if (redParam != null && (string.IsNullOrWhiteSpace(redParam.Text) || !redParam.Text.Contains("="))) redParam.Text = LanguageManager.GetText("LUTPage.Red.Params");
            var greenParam = (TextBlock)this.FindName("GreenParameterText");
            if (greenParam != null && (string.IsNullOrWhiteSpace(greenParam.Text) || !greenParam.Text.Contains("="))) greenParam.Text = LanguageManager.GetText("LUTPage.Green.Params");
            var blueParam = (TextBlock)this.FindName("BlueParameterText");
            if (blueParam != null && (string.IsNullOrWhiteSpace(blueParam.Text) || !blueParam.Text.Contains("="))) blueParam.Text = LanguageManager.GetText("LUTPage.Blue.Params");

            // Total Parameters - 항상 언어에 맞게 업데이트
            var totalBox = (TextBox)this.FindName("TotalParameterTextBox");
            if (totalBox != null && string.IsNullOrWhiteSpace(totalBox.Text)) totalBox.Text = LanguageManager.GetText("LUTPage.Total.Params");
        }

        // 캔버스 리사이즈 시 그래프를 다시 그려 반응형으로 보이게 함
        private void GraphCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Canvas canvas)
            {
                if (canvas.Name == "RedGraphCanvas" && _lastRed.HasValue)
                {
                    DrawLUTGraph(0, _lastRed.Value);
                }
                else if (canvas.Name == "GreenGraphCanvas" && _lastGreen.HasValue)
                {
                    DrawLUTGraph(1, _lastGreen.Value);
                }
                else if (canvas.Name == "BlueGraphCanvas" && _lastBlue.HasValue)
                {
                    DrawLUTGraph(2, _lastBlue.Value);
                }
            }
        }
        
        private void DrawLUTGraph(int rgbIndex, LUTParameter lutParam)
        {
            // 그래프 영역 찾기
            string canvasName = rgbIndex == 0 ? "RedGraphCanvas" : (rgbIndex == 1 ? "GreenGraphCanvas" : "BlueGraphCanvas");
            var canvas = (Canvas)this.FindName(canvasName);
            
            if (canvas != null)
            {
                canvas.Children.Clear();
                
                // 다크모드에서도 그래프 영역은 흰색 유지
                canvas.Background = Brushes.White;
                
                // Canvas의 실제 크기 가져오기
                double canvasWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth : 280;
                double canvasHeight = canvas.ActualHeight > 0 ? canvas.ActualHeight : 150;
                
                // 여백 설정 (Canvas 크기에 비례) - 살짝 늘려서 X,Y 라벨 공간 확보
                double margin = Math.Min(canvasWidth, canvasHeight) * 0.08; // 8% 여백으로 증가
                if (margin < 8) margin = 8; // 최소 8px
                if (margin > 25) margin = 25; // 최대 25px
                
                // 그래프 영역 계산 (영역을 넘어가지 않도록)
                double graphWidth = canvasWidth - 2 * margin;
                double graphHeight = canvasHeight - 2 * margin;
                
                // X축: 0부터 MAX_INDEX까지, Y축: 0부터 MAX_LUMI까지
                var points = new List<Point>();
                
                // X를 0부터 MAX_INDEX까지 세밀하게 계산하여 지수 곡선 생성
                int numPoints = 100; // 점의 개수
                for (int i = 0; i <= numPoints; i++)
                {
                    // X 범위: 0 ~ MAX_INDEX
                    double x = (double)i / numPoints * lutParam.max_index;
                    
                    // Y = max_lumi * (X/max_index)^gamma + black
                    double y = lutParam.max_lumi * Math.Pow(x / lutParam.max_index, lutParam.gamma) + lutParam.black;
                    
                    // Canvas 좌표로 변환 (영역 내에서만)
                    double pointX = margin + (x / lutParam.max_index) * graphWidth;
                    double pointY = canvasHeight - margin - (y / lutParam.max_lumi) * graphHeight;
                    
                    // 좌표가 그래프 영역을 벗어나지 않도록 엄격하게 제한
                    pointX = Math.Max(margin, Math.Min(canvasWidth - margin, pointX));
                    pointY = Math.Max(margin, Math.Min(canvasHeight - margin, pointY));
                    
                    points.Add(new Point(pointX, pointY));
                }
                
                // 지수 곡선 그리기
                if (points.Count > 0)
                {
                    var polyline = new Polyline
                    {
                        Stroke = rgbIndex == 0 ? Brushes.Red : (rgbIndex == 1 ? Brushes.Green : Brushes.Blue),
                        StrokeThickness = 2, // 선 두께 줄임
                        Points = new PointCollection(points)
                    };
                    
                    canvas.Children.Add(polyline);
                }
                
                // 1사분면 축 그리기 (X축: 아래쪽, Y축: 왼쪽)
                var xAxis = new Line
                {
                    X1 = margin,
                    Y1 = canvasHeight - margin,
                    X2 = canvasWidth - margin,
                    Y2 = canvasHeight - margin,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                
                var yAxis = new Line
                {
                    X1 = margin,
                    Y1 = margin,
                    X2 = margin,
                    Y2 = canvasHeight - margin,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                
                canvas.Children.Add(xAxis);
                canvas.Children.Add(yAxis);
                
                // X축 끝 값 표시 (MAX_INDEX) - 그래프 안쪽으로
                var xAxisLabel = new TextBlock
                {
                    Text = lutParam.max_index.ToString("F0"),
                    FontSize = 9,
                    Foreground = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Canvas.SetLeft(xAxisLabel, canvasWidth - margin - 25);
                Canvas.SetTop(xAxisLabel, canvasHeight - margin - 15);
                canvas.Children.Add(xAxisLabel);
                
                // Y축 끝 값 표시 (MAX_LUMI) - 그래프 안쪽으로
                var yAxisLabel = new TextBlock
                {
                    Text = lutParam.max_lumi.ToString("F0"),
                    FontSize = 9,
                    Foreground = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Canvas.SetLeft(yAxisLabel, margin + 5);
                Canvas.SetTop(yAxisLabel, margin + 5);
                canvas.Children.Add(yAxisLabel);
                
                // X축 라벨 - X축에서 충분히 떨어뜨리기
                var xLabel = new TextBlock
                {
                    Text = "X",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Canvas.SetLeft(xLabel, canvasWidth - margin - 7);
                Canvas.SetTop(xLabel, canvasHeight - margin +3);
                canvas.Children.Add(xLabel);
                
                // Y축 라벨 - 원래대로 유지
                var yLabel = new TextBlock
                {
                    Text = "Y",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Canvas.SetLeft(yLabel, margin - 10);
                Canvas.SetTop(yLabel, margin - 8);
                canvas.Children.Add(yLabel);
                
                // Gamma 값 표시 (곡선 옆에)
                if (points.Count > 0)
                {
                    // 곡선의 중간 지점에 gamma 값 표시
                    int middleIndex = points.Count / 2;
                    if (middleIndex < points.Count)
                    {
                        var gammaLabel = new TextBlock
                        {
                            Text = $"γ = {lutParam.gamma:F2}",
                            FontSize = 11,
                            FontWeight = FontWeights.Bold,
                            Foreground = rgbIndex == 0 ? Brushes.Red : (rgbIndex == 1 ? Brushes.Green : Brushes.Blue),
                            Background = Brushes.White,
                            Padding = new Thickness(4, 2, 4, 2)
                        };
                        
                        // 곡선에서 약간 오른쪽 위에 위치
                        Canvas.SetLeft(gammaLabel, points[middleIndex].X + 10);
                        Canvas.SetTop(gammaLabel, points[middleIndex].Y - 20);
                        canvas.Children.Add(gammaLabel);
                    }
                }
                
                // 디버그 메시지
                System.Diagnostics.Debug.WriteLine($"지수 그래프 그리기 완료: RGB={rgbIndex}, Canvas={canvasWidth}x{canvasHeight}, Margin={margin:F1}, X범위=0~{lutParam.max_index:F0}, Y범위=0~{lutParam.max_lumi:F0}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Canvas를 찾을 수 없음: {canvasName}");
            }
        }
        
        private void UpdateFormulaArea(int rgbIndex, LUTParameter lutParam)
        {
            string formulaTextName = rgbIndex == 0 ? "RedFormulaText" : (rgbIndex == 1 ? "GreenFormulaText" : "BlueFormulaText");
            var formulaText = (TextBlock)this.FindName(formulaTextName);
            
            if (formulaText != null)
            {
                // PPT처럼 수학 공식 표시 (분수와 지수 포함)
                formulaText.Inlines.Clear();
                
                // Y = 
                formulaText.Inlines.Add(new Run("Y = "));
                formulaText.Inlines.Add(new Run($"{lutParam.max_lumi:F2}"));
                formulaText.Inlines.Add(new Run(" × ("));
                
                // 분수 형태: X/MAX_INDEX
                var numeratorRun = new Run("X")
                {
                    BaselineAlignment = BaselineAlignment.Superscript,
                    FontSize = formulaText.FontSize * 0.8
                };
                var denominatorRun = new Run($"{lutParam.max_index:F0}")
                {
                    BaselineAlignment = BaselineAlignment.Subscript,
                    FontSize = formulaText.FontSize * 0.8
                };
                
                formulaText.Inlines.Add(numeratorRun);
                formulaText.Inlines.Add(new Run("/"));
                formulaText.Inlines.Add(denominatorRun);
                formulaText.Inlines.Add(new Run(")"));
                
                // 지수 부분을 위첨자로
                var exponentRun = new Run($"{lutParam.gamma:F2}")
                {
                    BaselineAlignment = BaselineAlignment.Superscript,
                    FontSize = formulaText.FontSize * 0.7
                };
                formulaText.Inlines.Add(exponentRun);
                
                formulaText.Inlines.Add(new Run($" + {lutParam.black:F2}"));
            }
        }
        
        private void UpdateParameterArea(int rgbIndex, LUTParameter lutParam)
        {
            string paramTextName = rgbIndex == 0 ? "RedParameterText" : (rgbIndex == 1 ? "GreenParameterText" : "BlueParameterText");
            var paramText = (TextBlock)this.FindName(paramTextName);
            
            if (paramText != null)
            {
                paramText.Text = $"MAX_LUMI = {lutParam.max_lumi:F2}\n" +
                               $"MAX_INDEX = {lutParam.max_index:F2}\n" +
                               $"GAMMA = {lutParam.gamma:F2}\n" +
                               $"BLACK = {lutParam.black:F2}";
            }
        }
        
        private void UpdateTotalParameters(LUTParameter[] lutParams)
        {
            var totalParamText = (TextBox)this.FindName("TotalParameterTextBox");
            if (totalParamText != null)
            {
                // 고정된 글자 크기로 설정 (기본 크기의 1.3배)
                totalParamText.FontSize = 12 * 1.3; // 15.6
                
                string totalText = "[LOOK_UP_TABLE]\n" +
                                 $"RED_MAX_LUMI = {lutParams[0].max_lumi:F2}\n" +
                                 $"RED_MAX_INDEX = {lutParams[0].max_index:F2}\n" +
                                 $"RED_GAMMA = {lutParams[0].gamma:F2}\n" +
                                 $"RED_BLACK = {lutParams[0].black:F2}\n" +
                                 $"GREEN_MAX_LUMI = {lutParams[1].max_lumi:F2}\n" +
                                 $"GREEN_MAX_INDEX = {lutParams[1].max_index:F2}\n" +
                                 $"GREEN_GAMMA = {lutParams[1].gamma:F2}\n" +
                                 $"GREEN_BLACK = {lutParams[1].black:F2}\n" +
                                 $"BLUE_MAX_LUMI = {lutParams[2].max_lumi:F2}\n" +
                                 $"BLUE_MAX_INDEX = {lutParams[2].max_index:F2}\n" +
                                 $"BLUE_GAMMA = {lutParams[2].gamma:F2}\n" +
                                 $"BLUE_BLACK = {lutParams[2].black:F2}";
                
                totalParamText.Text = totalText;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // 수동 검사 페이지와 완전히 동일한 로직
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.ShowMainPage();
            }
        }

        /// <summary>
        /// 테마 설정 (외부에서 호출 가능)
        /// </summary>
        /// <param name="isDarkMode">다크모드 여부</param>
        public void SetTheme(bool isDarkMode)
        {
            System.Diagnostics.Debug.WriteLine($"LUT 페이지 SetTheme 호출됨: {(_isDarkMode ? "다크" : "라이트")} -> {(isDarkMode ? "다크" : "라이트")}");
            _isDarkMode = isDarkMode;
            ApplyTheme();
            System.Diagnostics.Debug.WriteLine($"LUT 페이지 테마 적용 완료: {(_isDarkMode ? "다크" : "라이트")} 모드");
        }
    }
}

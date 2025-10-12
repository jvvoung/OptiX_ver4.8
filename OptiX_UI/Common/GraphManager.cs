using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace OptiX.Common
{
    /// <summary>
    /// 그래프 영역 관리 클래스 (OPTIC/IPVS 공통)
    /// </summary>
    public class GraphManager
    {
        private readonly Grid graphContent;
        private readonly ScrollViewer graphScrollViewer;
        private bool isDarkMode = false;
        private List<GraphDataPoint> cachedDataPoints = new List<GraphDataPoint>();

        // ViewModel 없이 사용 (IPVS용)
        public GraphManager(Grid graphContent, ScrollViewer graphScrollViewer)
        {
            this.graphContent = graphContent ?? throw new ArgumentNullException(nameof(graphContent));
            this.graphScrollViewer = graphScrollViewer;
        }

        // OPTIC 호환용 (기존 코드 유지)
        public GraphManager(Grid graphContent, ScrollViewer graphScrollViewer, object viewModel)
        {
            this.graphContent = graphContent ?? throw new ArgumentNullException(nameof(graphContent));
            this.graphScrollViewer = graphScrollViewer;
            // viewModel은 무시 (호환성 위해서만 존재)
        }

        /// <summary>
        /// 범용 그래프 데이터 포인트 클래스
        /// </summary>
        public class GraphDataPoint
        {
            public int ZoneNumber { get; set; }
            public string Judgment { get; set; }
            public int GlobalIndex { get; set; }
            public DateTime Timestamp { get; set; }  // OPTIC에서 사용
        }

        /// <summary>
        /// 다크모드 상태 설정 및 그래프 재생성
        /// </summary>
        public void SetDarkMode(bool darkMode)
        {
            this.isDarkMode = darkMode;
            
            // 다크모드 변경 시 그래프 재생성
            try
            {
                if (cachedDataPoints != null && cachedDataPoints.Count > 0)
                {
                    CreateDynamicGraph(cachedDataPoints);
                    System.Diagnostics.Debug.WriteLine($"다크모드 변경으로 그래프 재생성: {darkMode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"다크모드 그래프 갱신 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 그래프 영역 초기화 (처음 로드 시) - 원본 코드
        /// </summary>
        public void InitializeGraphArea()
        {
            try
            {
                if (graphContent == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ GraphContent를 찾을 수 없습니다!");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("✅ GraphContent 찾음 - 그래프 영역 초기화 시작");

                // 이미 생성되어 있으면 다시 생성하지 않음 (데이터 유지)
                if (graphContent.Children.Count != 0)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ 그래프 영역이 이미 생성됨 - 스킵");
                    RestoreExistingGraphData();
                    return;
                }

                // 빈 데이터 포인트로 초기 그래프 생성
                CreateDynamicGraph(new List<GraphDataPoint>());

                // 기존 데이터 복원
                RestoreExistingGraphData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"그래프 영역 초기화 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 페이지 진입 시 기존 Graph 데이터 자동 복원 (원본 코드)
        /// </summary>
        public void RestoreExistingGraphData()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("RestoreExistingGraphData: 복원할 데이터 없음");

                if (cachedDataPoints == null || cachedDataPoints.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("복원할 Graph 데이터가 없습니다.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"복원할 Graph 데이터: {cachedDataPoints.Count}개 포인트");

                // 기존 데이터를 사용하여 그래프 업데이트
                CreateDynamicGraph(cachedDataPoints);

                System.Diagnostics.Debug.WriteLine("기존 Graph 데이터 복원 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"기존 Graph 데이터 복원 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Zone 개수에 따라 동적으로 그래프 생성 (원본 코드)
        /// </summary>
        public void CreateDynamicGraph(List<GraphDataPoint> dataPoints)
        {
            try
            {
                if (graphContent == null) return;

                // 기존 그래프 내용 클리어
                graphContent.Children.Clear();
                graphContent.RowDefinitions.Clear();
                
                // ScrollViewer의 상단 간격 조정 (부모 Grid의 Margin="12" 상쇄)
                if (graphScrollViewer != null)
                {
                    graphScrollViewer.Margin = new Thickness(0, -6, 0, -6);
                }

                // Zone 개수 읽기
                int zoneCount = 2;
                try
                {
                    string zoneCountStr = GlobalDataManager.GetValue("Settings", "MTP_ZONE", "2");
                    if (!int.TryParse(zoneCountStr, out zoneCount)) zoneCount = 2;
                }
                catch { zoneCount = 2; }

                // Zone별 그래프 행 생성 (ZONE 안에 OK/R/J/PTN 포함)
                for (int zone = 1; zone <= zoneCount; zone++)
                {
                    CreateZoneGraphRow(graphContent, zone, dataPoints);
                }
                
                // 데이터 포인트가 있으면 그래프에 그리기 (Canvas 렌더링 후)
                if (dataPoints != null && dataPoints.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"CreateDynamicGraph: {dataPoints.Count}개 데이터 포인트를 그래프에 그립니다.");
                    
                    // Canvas가 렌더링될 때까지 기다린 후 데이터 포인트 그리기
                    graphContent.Dispatcher.InvokeAsync(() =>
                    {
                        UpdateGraphDataPoints(dataPoints);
                    }, System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"동적 그래프 생성 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Zone별 그래프 행 생성 (이미지 레이아웃 - ZONE 안에 OK/R/J/PTN 포함)
        /// </summary>
        private void CreateZoneGraphRow(Grid parentGrid, int zoneNumber, List<GraphDataPoint> dataPoints)
        {
            try
            {
                parentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var zoneBorder = new Border
                {
                    Background = isDarkMode ? new SolidColorBrush(Color.FromRgb(30, 30, 30)) : new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    BorderBrush = isDarkMode ? new SolidColorBrush(Color.FromRgb(64, 64, 64)) : new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(3, 0.5, 3, 0.5),
                    Padding = new Thickness(3)
                };

                var zoneInnerGrid = new Grid();

                // ZONE 라벨 행
                zoneInnerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
                var zoneLabel = new TextBlock
                {
                    Text = $"ZONE{zoneNumber}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Foreground = isDarkMode ? Brushes.White : new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 1)
                };
                Grid.SetRow(zoneLabel, 0);
                zoneInnerGrid.Children.Add(zoneLabel);

                // OK, R/J, PTN 행 추가
                string[] judgmentTypes = { "OK", "R/J", "PTN" };
                Color[] judgmentColors = { 
                    Color.FromRgb(76, 175, 80),
                    Color.FromRgb(244, 67, 54),
                    Color.FromRgb(255, 193, 7)
                };

                for (int i = 0; i < judgmentTypes.Length; i++)
                {
                    zoneInnerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var judgmentGrid = new Grid
                    {
                        Margin = new Thickness(0, 0, 0, 0)
                    };
                    judgmentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
                    judgmentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var judgmentLabel = new TextBlock
                    {
                        Text = judgmentTypes[i],
                        FontWeight = FontWeights.Bold,
                        FontSize = 10,
                        Foreground = new SolidColorBrush(judgmentColors[i]),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextAlignment = TextAlignment.Center
                    };
                    Grid.SetColumn(judgmentLabel, 0);
                    judgmentGrid.Children.Add(judgmentLabel);

                    // 판정 색상 테두리가 있는 Border로 감싸기
                    var canvasBorder = new Border
                    {
                        BorderBrush = new SolidColorBrush(judgmentColors[i]),
                        BorderThickness = new Thickness(1),
                        Background = isDarkMode 
                            ? new SolidColorBrush(Color.FromRgb(30, 30, 30))  // 다크모드: 어두운 배경
                            : new SolidColorBrush(Colors.White),  // 화이트모드: 흰색 배경
                        CornerRadius = new CornerRadius(2),
                        Margin = new Thickness(2)
                    };
                    Grid.SetColumn(canvasBorder, 1);

                    var canvasContainer = new Grid();
                    canvasBorder.Child = canvasContainer;

                    string judgmentName = judgmentTypes[i].Replace("/", "");
                    var dataCanvas = new Canvas
                    {
                        MinHeight = 22,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Background = Brushes.Transparent,
                        Name = $"Zone{zoneNumber}_{judgmentName}GraphCanvas",
                        ClipToBounds = true
                    };
                    canvasContainer.Children.Add(dataCanvas);
                    System.Diagnostics.Debug.WriteLine($"Canvas 생성: Zone{zoneNumber}_{judgmentName}GraphCanvas");

                    judgmentGrid.Children.Add(canvasBorder);
                    Grid.SetRow(judgmentGrid, i + 1);
                    zoneInnerGrid.Children.Add(judgmentGrid);
                }

                zoneBorder.Child = zoneInnerGrid;
                Grid.SetRow(zoneBorder, zoneNumber - 1);
                parentGrid.Children.Add(zoneBorder);

                System.Diagnostics.Debug.WriteLine($"Zone{zoneNumber} 생성 완료 (OK/R/J/PTN 포함)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zone 그래프 행 생성 오류 (Zone{zoneNumber}): {ex.Message}");
            }
        }




        /// <summary>
        /// 그래프 데이터 클리어
        /// </summary>
        public void ClearGraphData()
        {
            try
            {
                if (graphContent != null)
                {
                    CreateDynamicGraph(new List<GraphDataPoint>());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"그래프 클리어 오류: {ex.Message}");
            }
        }

        #region 그래프 Canvas 렌더링 (OpticPage.xaml.cs에서 이동)

        /// <summary>
        /// 그래프 표시 업데이트 (OpticPage.xaml.cs 508-539줄에서 이동)
        /// </summary>
        public void UpdateGraphDisplay(List<GraphDataPoint> dataPoints)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"그래프 표시 업데이트 호출됨: {dataPoints?.Count ?? 0}개 데이터 포인트");

                // Graph 탭이 활성화되어 있을 때만 업데이트
                if (graphContent == null)
                {
                    System.Diagnostics.Debug.WriteLine("GraphContent를 찾을 수 없음");
                    return;
                }

                // cachedDataPoints 업데이트 (다크모드 전환 시 필요)
                if (dataPoints != null)
                {
                    cachedDataPoints = new List<GraphDataPoint>(dataPoints);
                    System.Diagnostics.Debug.WriteLine($"✅ cachedDataPoints 업데이트: {cachedDataPoints.Count}개");
                }

                // 기존 그래프 영역에 데이터 포인트만 업데이트 (빠른 업데이트)
                UpdateGraphDataPoints(dataPoints);
                
                if (graphContent.Visibility != Visibility.Visible)
                {
                    System.Diagnostics.Debug.WriteLine("Graph 탭이 현재 보이지 않음 - 데이터는 업데이트됨, 탭 전환 시 표시됨");
                }
                System.Diagnostics.Debug.WriteLine("그래프 표시 업데이트 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"그래프 표시 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 기존 그래프 영역에 데이터 포인트만 업데이트 (OpticPage.xaml.cs 544-595줄에서 이동)
        /// </summary>
        private void UpdateGraphDataPoints(List<GraphDataPoint> dataPoints)
        {
            try
            {
                if (graphContent == null) 
                {
                    System.Diagnostics.Debug.WriteLine("GraphContent를 찾을 수 없습니다!");
                    return;
                }

                // 그래프 영역이 초기화되지 않았으면 초기화
                if (graphContent.Children.Count == 0)
                {
                    InitializeGraphArea();
                }

                // ScrollViewer에서 Canvas들 찾기
                var allCanvases = new List<Canvas>();
                FindCanvasesInPanel(graphContent, allCanvases);

                System.Diagnostics.Debug.WriteLine($"UpdateGraphDataPoints: 찾은 Canvas 개수 = {allCanvases.Count}, 데이터 포인트 개수 = {dataPoints?.Count ?? 0}");

                // Zone별 Judgment Canvas 찾기
                var zoneJudgmentCanvases = allCanvases.Where(c => !string.IsNullOrEmpty(c.Name) && c.Name.Contains("Zone") && c.Name.Contains("GraphCanvas")).ToList();

                // 각 Zone별 Judgment별로 데이터 포인트 그리기
                foreach (var canvas in zoneJudgmentCanvases)
                {
                    canvas.Children.Clear(); // 기존 포인트 제거
                    
                    // Canvas 이름 파싱: Zone{number}_{judgment}GraphCanvas
                    var nameParts = canvas.Name.Replace("GraphCanvas", "").Split('_');
                    if (nameParts.Length == 2)
                    {
                        var zoneNumber = int.Parse(nameParts[0].Replace("Zone", ""));
                        var judgmentType = nameParts[1];
                        
                        // R/J를 RJ로 변환하여 매칭
                        string searchJudgment = judgmentType == "RJ" ? "R/J" : judgmentType;
                        var filteredPoints = dataPoints.Where(p => p.ZoneNumber == zoneNumber && p.Judgment == searchJudgment).ToList();
                        
                        DrawDataPointsOnCanvas(canvas, filteredPoints, $"{zoneNumber}_{judgmentType}");
                    }
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"그래프 데이터 포인트 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Panel에서 모든 Canvas를 재귀적으로 찾기 (OpticPage.xaml.cs 600-625줄에서 이동)
        /// </summary>
        private void FindCanvasesInPanel(Panel panel, List<Canvas> canvasList)
        {
            foreach (var child in panel.Children)
            {
                if (child is Canvas canvas)
                {
                    canvasList.Add(canvas);
                }
                else if (child is Panel childPanel)
                {
                    FindCanvasesInPanel(childPanel, canvasList);
                }
                else if (child is Border border && border.Child is Panel borderPanel)
                {
                    FindCanvasesInPanel(borderPanel, canvasList);
                }
                else if (child is Grid grid)
                {
                    FindCanvasesInPanel(grid, canvasList);
                }
                else if (child is ScrollViewer scrollViewer && scrollViewer.Content is Panel scrollPanel)
                {
                    FindCanvasesInPanel(scrollPanel, canvasList);
                }
            }
        }

        /// <summary>
        /// Canvas에 데이터 포인트 그리기 (OpticPage.xaml.cs 630-699줄에서 이동)
        /// </summary>
        private void DrawDataPointsOnCanvas(Canvas canvas, List<GraphDataPoint> points, object identifier)
        {
            try
            {
                if (points == null || points.Count == 0) return;

                var maxPoints = 300;
                var canvasWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth : 600;
                
                // Canvas 높이 계산 (ActualHeight가 0이면 MinHeight 사용, 그것도 없으면 기본값 22)
                double canvasHeight = canvas.ActualHeight;
                if (canvasHeight <= 0)
                {
                    canvasHeight = canvas.MinHeight > 0 ? canvas.MinHeight : 22;
                }

                // 최근 포인트만 사용
                var recentPoints = points.Skip(Math.Max(0, points.Count - maxPoints)).ToList();

                // Zone별 인덱스 계산 (각 Zone의 OK, R/J, PTN이 같은 인덱스 공유)
                if (cachedDataPoints == null || cachedDataPoints.Count == 0) return;

                // 현재 Zone의 모든 포인트를 시간순 정렬
                var currentZone = points.Count > 0 ? points[0].ZoneNumber : 0;
                var zoneAllPoints = cachedDataPoints
                    .Where(dp => dp.ZoneNumber == currentZone)
                    .OrderBy(dp => dp.GlobalIndex)
                    .ToList();

                for (int i = 0; i < recentPoints.Count; i++)
                {
                    var point = recentPoints[i];
                    
                    // Zone 내에서의 인덱스 찾기
                    var zoneIndex = zoneAllPoints.FindIndex(dp => 
                        dp.ZoneNumber == point.ZoneNumber && 
                        dp.Judgment == point.Judgment && 
                        dp.GlobalIndex == point.GlobalIndex);
                    
                    if (zoneIndex >= 0)
                    {
                        double availableWidth = (canvasWidth - 10)*2;
                        double stepWidth = availableWidth / 300.0;
                        double x = zoneIndex * stepWidth + 5;
                        
                        if (zoneIndex >= 299)
                        {
                            x = canvasWidth - 5;
                        }
                        
                        double y = canvasHeight / 2;

                        var brush = GetJudgmentColor(point.Judgment);
                        
                        var ellipse = new Ellipse
                        {
                            Width = 6,
                            Height = 6,
                            Fill = brush,
                            Stroke = new SolidColorBrush(Colors.Black),
                            StrokeThickness = 0.5
                        };

                        Canvas.SetLeft(ellipse, x - 3);
                        Canvas.SetTop(ellipse, y - 3);
                        canvas.Children.Add(ellipse);
                    }
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Canvas 그리기 오류 ({identifier}): {ex.Message}");
            }
        }

        /// <summary>
        /// 판정 색상 반환 (OpticPage.xaml.cs 704-719줄에서 이동)
        /// </summary>
        private SolidColorBrush GetJudgmentColor(string judgment)
        {
            switch (judgment)
            {
                case "OK":
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80));
                case "NG":
                case "R/J":
                case "RJ":
                    return new SolidColorBrush(Color.FromRgb(244, 67, 54));
                case "PTN":
                    return new SolidColorBrush(Color.FromRgb(255, 193, 7));
                default:
                    return new SolidColorBrush(Color.FromRgb(158, 158, 158));
            }
        }

        #endregion

        #region 그래프 데이터 포인트 관리 (ViewModel에서 이동)

        private List<GraphDataPoint> viewModelDataPoints = new List<GraphDataPoint>();

        /// <summary>
        /// 그래프 데이터 포인트 추가 (OPTIC/IPVS 공통)
        /// </summary>
        /// <param name="zoneNumber">Zone 번호</param>
        /// <param name="judgment">판정 결과 (OK/NG/PTN/R/J)</param>
        /// <param name="includeTimestamp">Timestamp 포함 여부 (OPTIC: true, IPVS: false)</param>
        /// <returns>추가된 데이터 포인트 리스트</returns>
        public List<GraphDataPoint> AddDataPoint(int zoneNumber, string judgment, bool includeTimestamp = true)
        {
            try
            {
                // 새로운 데이터 포인트 추가
                var newPoint = new GraphDataPoint
                {
                    ZoneNumber = zoneNumber,
                    Judgment = judgment,
                    Timestamp = includeTimestamp ? DateTime.Now : default(DateTime),
                    GlobalIndex = viewModelDataPoints.Count  // 현재 개수를 GlobalIndex로 설정
                };

                viewModelDataPoints.Add(newPoint);

                // 최대 MAX_GRAPH_DATA_POINTS개까지만 유지 (FIFO) - 메모리 누수 방지
                if (viewModelDataPoints.Count > DLL.DllConstants.MAX_GRAPH_DATA_POINTS)
                {
                    viewModelDataPoints.RemoveAt(0);
                    
                    // GlobalIndex 재조정 (0부터 다시 시작)
                    for (int i = 0; i < viewModelDataPoints.Count; i++)
                    {
                        viewModelDataPoints[i].GlobalIndex = i;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"그래프 데이터 포인트 추가: Zone{zoneNumber} - {judgment} (총 {viewModelDataPoints.Count}개, GlobalIndex: {newPoint.GlobalIndex})");

                // cachedDataPoints도 업데이트 (그래프 렌더링용)
                cachedDataPoints = new List<GraphDataPoint>(viewModelDataPoints);
                
                return new List<GraphDataPoint>(viewModelDataPoints);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"그래프 데이터 포인트 추가 오류: {ex.Message}");
                return new List<GraphDataPoint>(viewModelDataPoints);
            }
        }

        /// <summary>
        /// 그래프 데이터 초기화
        /// </summary>
        public void ClearDataPoints()
        {
            try
            {
                viewModelDataPoints.Clear();
                cachedDataPoints.Clear();
                System.Diagnostics.Debug.WriteLine("그래프 데이터 포인트 초기화 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"그래프 데이터 초기화 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 현재 그래프 데이터 포인트 가져오기
        /// </summary>
        public List<GraphDataPoint> GetDataPoints()
        {
            return new List<GraphDataPoint>(viewModelDataPoints);
        }

        /// <summary>
        /// 특정 Zone의 판정 결과 반환
        /// </summary>
        public string GetJudgmentForZone(int zoneNumber, System.Collections.ObjectModel.ObservableCollection<DataTableItem> dataItems)
        {
            try
            {
                // Zone별 판정 결과 찾기 (Zone은 string 타입)
                var zoneData = dataItems?.Where(item => item.Zone == zoneNumber.ToString()).FirstOrDefault();
                return zoneData?.Judgment ?? "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} 판정 결과 조회 오류: {ex.Message}");
                return "";
            }
        }

        #endregion
    }
}

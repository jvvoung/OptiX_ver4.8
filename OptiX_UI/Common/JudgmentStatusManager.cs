using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OptiX.Common
{
    /// <summary>
    /// 판정 현황 테이블 관리 클래스 (OPTIC/IPVS 공통)
    /// 
    /// 역할:
    /// - 판정 현황 테이블 생성 (OK, R/J, PTN 행)
    /// - 판정 현황 업데이트 (수량, 비율)
    /// - Total, Pass, Fail 집계
    /// - 다크모드 전환 시 색상 업데이트
    /// 
    /// 사용하는 UI 요소:
    /// - 판정 현황 TextBlock들 (TotalQuantity, TotalRate, OKQuantity, OKRate, RJQuantity, RJRate, PTNQuantity, PTNRate)
    /// 
    /// 판정 현황 행:
    /// - Total: 전체 개수
    /// - OK: 합격 개수
    /// - R/J: 불합격 개수 (Reject/Judgment)
    /// - PTN: Pattern 개수
    /// </summary>
    public class JudgmentStatusManager
    {
        // 판정 현황 TextBlock들을 Dictionary로 관리
        private readonly Dictionary<string, (TextBlock quantity, TextBlock rate)> statusTextBlocks;
        private readonly Grid judgmentStatusContainer;
        private readonly UserControl page; // 동적 탐색용 (OPTIC/IPVS Page)
        private bool isDarkMode = false;

        public JudgmentStatusManager(
            Dictionary<string, (TextBlock quantity, TextBlock rate)> statusTextBlocks,
            Grid judgmentStatusContainer,
            UserControl page)
        {
            this.statusTextBlocks = statusTextBlocks ?? throw new ArgumentNullException(nameof(statusTextBlocks));
            this.judgmentStatusContainer = judgmentStatusContainer;
            this.page = page; // ClearJudgmentStatus의 동적 탐색용
        }

        /// <summary>
        /// 다크모드 상태 설정
        /// </summary>
        public void SetDarkMode(bool darkMode)
        {
            this.isDarkMode = darkMode;
        }

        /// <summary>
        /// 판정 현황 행 업데이트 (OpticPage.xaml.cs 2304~2353줄에서 복사)
        /// </summary>
        public void UpdateJudgmentStatusRow(string rowName, string quantity, string rate)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"판정 현황 업데이트: {rowName} - 수량: {quantity}, 비율: {rate}");
                
                // Dictionary에서 TextBlock 가져와서 업데이트
                if (statusTextBlocks.TryGetValue(rowName, out var textBlocks))
                {
                    if (textBlocks.quantity != null) textBlocks.quantity.Text = quantity;
                    if (textBlocks.rate != null) textBlocks.rate.Text = rate;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ 판정 현황 TextBlock 못 찾음: {rowName}");
                }
                
                System.Diagnostics.Debug.WriteLine($"판정 현황 업데이트 완료: {rowName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"판정 현황 행 업데이트 오류 ({rowName}): {ex.Message}");
            }
        }

        /// <summary>
        /// 판정 현황 표의 TextBlock들을 직접 찾아서 업데이트 (OpticPage.xaml.cs 2355~2403줄에서 복사)
        /// </summary>
        private void UpdateJudgmentStatusTextBlocks(string rowName, string quantity, string rate)
        {
            try
            {
                // 판정 현황 컨테이너 확인 (생성자에서 받은 것 사용)
                Grid container = judgmentStatusContainer;
                
                if (container == null)
                {
                    // 대안: 하단 가운데 컬럼에서 찾기 (로컬 변수 사용)
                    var mainGrid = page.Content as Grid;
                    if (mainGrid != null && mainGrid.Children.Count > 0)
                    {
                        var bottomGrid = mainGrid.Children.OfType<Grid>().FirstOrDefault();
                        if (bottomGrid != null && bottomGrid.ColumnDefinitions.Count >= 3)
                        {
                            // 가운데 컬럼 (인덱스 1)에서 판정 현황 표 찾기
                            var middleColumnChildren = bottomGrid.Children.Cast<UIElement>()
                                .Where(child => Grid.GetColumn(child) == 1).ToList();
                            
                            foreach (var child in middleColumnChildren)
                            {
                                if (child is Border border)
                                {
                                    container = FindGridInBorder(border);
                                    if (container != null) break;
                                }
                            }
                        }
                    }
                }

                if (container != null)
                {
                    UpdateTextBlocksInGrid(container, rowName, quantity, rate);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("판정 현황 표 컨테이너를 찾을 수 없음");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"판정 현황 TextBlock 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Border 내부에서 Grid 찾기 (OpticPage.xaml.cs 2405~2420줄에서 복사)
        /// </summary>
        private Grid FindGridInBorder(Border border)
        {
            if (border.Child is Grid grid)
            {
                return grid;
            }
            else if (border.Child is FrameworkElement element)
            {
                // 재귀적으로 Grid 찾기
                return FindGridInElement(element);
            }
            return null;
        }

        /// <summary>
        /// FrameworkElement 내부에서 Grid 찾기 (OpticPage.xaml.cs 2422~2440줄에서 복사)
        /// </summary>
        private Grid FindGridInElement(FrameworkElement element)
        {
            if (element is Grid grid)
            {
                return grid;
            }
            else if (element is Panel panel)
            {
                foreach (var child in panel.Children.OfType<FrameworkElement>())
                {
                    var found = FindGridInElement(child);
                    if (found != null) return found;
                }
            }
            return null;
        }

        /// <summary>
        /// Grid 내부의 TextBlock들 업데이트 (OpticPage.xaml.cs 2442~2474줄에서 복사)
        /// </summary>
        private void UpdateTextBlocksInGrid(Grid grid, string rowName, string quantity, string rate)
        {
            try
            {
                // Grid의 모든 TextBlock 찾기
                var allTextBlocks = FindAllTextBlocks(grid);
                
                // 행 이름에 해당하는 TextBlock들 찾기
                var targetRow = allTextBlocks.Where(tb => tb.Text == rowName).FirstOrDefault();
                if (targetRow != null)
                {
                    // 같은 행의 다른 TextBlock들 찾기
                    var rowIndex = Grid.GetRow(targetRow);
                    var rowTextBlocks = allTextBlocks.Where(tb => Grid.GetRow(tb) == rowIndex).ToList();
                    
                    if (rowTextBlocks.Count >= 3)
                    {
                        // 수량과 발생률 TextBlock 업데이트
                        rowTextBlocks[1].Text = quantity; // 수량
                        rowTextBlocks[2].Text = rate;     // 발생률
                        
                        System.Diagnostics.Debug.WriteLine($"판정 현황 업데이트 완료: {rowName} - {quantity}, {rate}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Grid 내부 TextBlock 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Grid 내부의 모든 TextBlock 찾기 (OpticPage.xaml.cs 2476~2498줄에서 복사)
        /// </summary>
        private List<TextBlock> FindAllTextBlocks(DependencyObject parent)
        {
            var textBlocks = new List<TextBlock>();
            
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is TextBlock textBlock)
                {
                    textBlocks.Add(textBlock);
                }
                else
                {
                    textBlocks.AddRange(FindAllTextBlocks(child));
                }
            }
            
            return textBlocks;
        }

        /// <summary>
        /// 판정 현황 표 그리드 찾기 (OpticPage.xaml.cs 2500~2524줄에서 복사)
        /// </summary>
        private Grid FindStatusTableGrid()
        {
            try
            {
                // XAML에서 정의된 판정 현황 표의 그리드 찾기
                // 판정 현황 표는 하단 가운데 영역에 위치
                var judgmentStatusPanel = page.FindName("JudgmentStatusPanel") as Grid;
                if (judgmentStatusPanel != null)
                {
                    return judgmentStatusPanel;
                }
                
                // 대안: LogicalTreeHelper를 사용하여 찾기
                var statusTable = LogicalTreeHelper.FindLogicalNode(page, "JudgmentStatusTable") as Grid;
                return statusTable;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"판정 현황 표 그리드 찾기 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 판정 현황 표 행 인덱스 가져오기 (OpticPage.xaml.cs 2526~2539줄에서 복사)
        /// </summary>
        private int GetStatusTableRowIndex(string rowName)
        {
            switch (rowName)
            {
                case "Total": return 0;
                case "PTN": return 1;
                case "R/J": return 2;
                case "OK": return 3;
                default: return -1;
            }
        }

        /// <summary>
        /// 판정 현황 표 셀 업데이트 (OpticPage.xaml.cs 2541~2568줄에서 복사)
        /// </summary>
        private void UpdateStatusTableCell(Grid grid, int row, int column, string value)
        {
            try
            {
                // 그리드에서 해당 위치의 TextBlock 찾아서 업데이트
                var children = grid.Children.Cast<UIElement>().Where(child => 
                    Grid.GetRow(child) == row && Grid.GetColumn(child) == column).ToList();
                
                foreach (var child in children)
                {
                    if (child is Border border && border.Child is Grid innerGrid)
                    {
                        var textBlocks = innerGrid.Children.OfType<TextBlock>().ToList();
                        if (textBlocks.Count > column)
                        {
                            textBlocks[column].Text = value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"판정 현황 표 셀 업데이트 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 판정 현황 테이블 초기화
        /// </summary>
        public void ClearJudgmentStatus()
        {
            try
            {
                UpdateJudgmentStatusRow("Total", "0", "1.00");
                UpdateJudgmentStatusRow("OK", "0", "0.00");
                UpdateJudgmentStatusRow("R/J", "0", "0.00");
                UpdateJudgmentStatusRow("PTN", "0", "0.00");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"판정 현황 초기화 오류: {ex.Message}");
            }
        }
    }
}



using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OptiX.Main
{
    /// <summary>
    /// MainWindow의 창 크기 조정 및 최대화/복원 관리 클래스
    /// 
    /// 역할:
    /// - 창 최대화/복원 토글
    /// - 창 크기 조정 (8방향 리사이즈 핸들)
    /// - 최대화 버튼 상태 업데이트
    /// </summary>
    public class WindowResizeManager
    {
        private readonly Window mainWindow;
        
        private bool isMaximized = false;
        private bool isResizing = false;
        private Point resizeStartPoint;
        private Size resizeStartSize;
        private string resizeDirection = "";

        public WindowResizeManager(Window window)
        {
            this.mainWindow = window;
            
            // 창 상태 변경 이벤트 구독
            mainWindow.StateChanged += OnWindowStateChanged;
        }

        /// <summary>
        /// 창 최대화/복원 토글
        /// </summary>
        public void ToggleMaximize()
        {
            if (isMaximized)
            {
                // 복원
                mainWindow.WindowState = WindowState.Normal;
                isMaximized = false;
                UpdateMaximizeButton();
            }
            else
            {
                // 최대화
                mainWindow.WindowState = WindowState.Maximized;
                isMaximized = true;
                UpdateMaximizeButton();
            }
        }

        /// <summary>
        /// 최대화 버튼 아이콘 업데이트
        /// </summary>
        private void UpdateMaximizeButton()
        {
            var maximizeButton = mainWindow.FindName("MaximizeButton") as Button;
            if (maximizeButton != null)
            {
                maximizeButton.Content = isMaximized ? "❐" : "□";
            }
        }

        /// <summary>
        /// 창 상태 변경 이벤트 핸들러
        /// </summary>
        private void OnWindowStateChanged(object sender, EventArgs e)
        {
            // 창 상태가 변경될 때 최대화 상태 업데이트
            if (mainWindow.WindowState == WindowState.Maximized)
            {
                isMaximized = true;
            }
            else if (mainWindow.WindowState == WindowState.Normal)
            {
                isMaximized = false;
            }

            UpdateMaximizeButton();
        }

        #region 창 크기 조정 이벤트

        /// <summary>
        /// 리사이즈 핸들 마우스 버튼 다운 이벤트
        /// </summary>
        public void OnResizeHandleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isMaximized) return; // 최대화 상태에서는 크기 조정 불가

            isResizing = true;
            resizeStartPoint = e.GetPosition(mainWindow);
            resizeStartSize = new Size(mainWindow.Width, mainWindow.Height);

            var handle = sender as Border;
            if (handle != null)
            {
                switch (handle.Name)
                {
                    case "TopResizeHandle":
                        resizeDirection = "Top";
                        break;
                    case "BottomResizeHandle":
                        resizeDirection = "Bottom";
                        break;
                    case "LeftResizeHandle":
                        resizeDirection = "Left";
                        break;
                    case "RightResizeHandle":
                        resizeDirection = "Right";
                        break;
                    case "TopLeftResizeHandle":
                        resizeDirection = "TopLeft";
                        break;
                    case "TopRightResizeHandle":
                        resizeDirection = "TopRight";
                        break;
                    case "BottomLeftResizeHandle":
                        resizeDirection = "BottomLeft";
                        break;
                    case "BottomRightResizeHandle":
                        resizeDirection = "BottomRight";
                        break;
                    default:
                        resizeDirection = "";
                        break;
                }

                // 클릭한 상태에서만 커서 변경
                if (resizeDirection == "Top" || resizeDirection == "Bottom")
                {
                    mainWindow.Cursor = Cursors.SizeNS;
                }
                else if (resizeDirection == "Left" || resizeDirection == "Right")
                {
                    mainWindow.Cursor = Cursors.SizeWE;
                }
                else if (resizeDirection == "TopLeft" || resizeDirection == "BottomRight")
                {
                    mainWindow.Cursor = Cursors.SizeNWSE;
                }
                else if (resizeDirection == "TopRight" || resizeDirection == "BottomLeft")
                {
                    mainWindow.Cursor = Cursors.SizeNESW;
                }
                else
                {
                    mainWindow.Cursor = Cursors.Arrow;
                }
            }

            mainWindow.CaptureMouse();
            e.Handled = true;
        }

        /// <summary>
        /// 리사이즈 핸들 마우스 이동 이벤트
        /// </summary>
        public void OnResizeHandleMouseMove(object sender, MouseEventArgs e)
        {
            if (!isResizing || isMaximized) return;

            var currentPoint = e.GetPosition(mainWindow);
            var deltaX = currentPoint.X - resizeStartPoint.X;
            var deltaY = currentPoint.Y - resizeStartPoint.Y;

            // 부드러운 크기 조정을 위해 직접 계산
            var newWidth = resizeStartSize.Width;
            var newHeight = resizeStartSize.Height;
            var newLeft = mainWindow.Left;
            var newTop = mainWindow.Top;

            switch (resizeDirection)
            {
                case "Top":
                    newHeight = Math.Max(mainWindow.MinHeight, resizeStartSize.Height - deltaY);
                    newTop = mainWindow.Top + (resizeStartSize.Height - newHeight);
                    break;
                case "Bottom":
                    newHeight = Math.Max(mainWindow.MinHeight, resizeStartSize.Height + deltaY);
                    break;
                case "Left":
                    newWidth = Math.Max(mainWindow.MinWidth, resizeStartSize.Width - deltaX);
                    newLeft = mainWindow.Left + (resizeStartSize.Width - newWidth);
                    break;
                case "Right":
                    newWidth = Math.Max(mainWindow.MinWidth, resizeStartSize.Width + deltaX);
                    break;
                case "TopLeft":
                    newWidth = Math.Max(mainWindow.MinWidth, resizeStartSize.Width - deltaX);
                    newHeight = Math.Max(mainWindow.MinHeight, resizeStartSize.Height - deltaY);
                    newLeft = mainWindow.Left + (resizeStartSize.Width - newWidth);
                    newTop = mainWindow.Top + (resizeStartSize.Height - newHeight);
                    break;
                case "TopRight":
                    newWidth = Math.Max(mainWindow.MinWidth, resizeStartSize.Width + deltaX);
                    newHeight = Math.Max(mainWindow.MinHeight, resizeStartSize.Height - deltaY);
                    newTop = mainWindow.Top + (resizeStartSize.Height - newHeight);
                    break;
                case "BottomLeft":
                    newWidth = Math.Max(mainWindow.MinWidth, resizeStartSize.Width - deltaX);
                    newHeight = Math.Max(mainWindow.MinHeight, resizeStartSize.Height + deltaY);
                    newLeft = mainWindow.Left + (resizeStartSize.Width - newWidth);
                    break;
                case "BottomRight":
                    newWidth = Math.Max(mainWindow.MinWidth, resizeStartSize.Width + deltaX);
                    newHeight = Math.Max(mainWindow.MinHeight, resizeStartSize.Height + deltaY);
                    break;
            }

            // 즉시 크기 업데이트
            mainWindow.Width = newWidth;
            mainWindow.Height = newHeight;
            mainWindow.Left = newLeft;
            mainWindow.Top = newTop;

            e.Handled = true;
        }

        /// <summary>
        /// 리사이즈 핸들 마우스 버튼 업 이벤트
        /// </summary>
        public void OnResizeHandleMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isResizing)
            {
                isResizing = false;
                mainWindow.Cursor = Cursors.Arrow; // 커서를 기본 화살표로 되돌리기
                mainWindow.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        #endregion
    }
}




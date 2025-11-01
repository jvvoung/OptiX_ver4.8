using System;
using System.Windows;
using System.Windows.Controls;

namespace OptiX.Main
{
    /// <summary>
    /// MainWindow의 페이지 네비게이션 관리 클래스
    /// 
    /// 역할:
    /// - 페이지 Lazy Loading (처음 사용 시에만 생성, 이후 재사용)
    /// - 페이지 전환 로직 캡슐화
    /// - 다크모드 상태 전달
    /// - 현재 페이지 관리
    /// </summary>
    public class PageNavigationManager
    {
        private readonly Window mainWindow;
        private readonly Grid mainContent;
        private readonly Grid mainPageContent;
        
        private UserControl currentPage;
        private bool isDarkMode = false;
        
        // 페이지 캐싱 (Lazy Loading)
        private OpticPage _opticPage;
        private IPVSPage _ipvsPage;
        private LUTPage _lutPage;
        private ManualPage _manualPage;

        public PageNavigationManager(Window window)
        {
            this.mainWindow = window;
            this.mainContent = mainWindow.FindName("MainContent") as Grid;
            this.mainPageContent = mainWindow.FindName("MainPageContent") as Grid;
            
            if (mainContent == null)
            {
                throw new InvalidOperationException("MainContent Grid를 찾을 수 없습니다.");
            }
        }

        /// <summary>
        /// 다크모드 상태 설정
        /// </summary>
        public void SetDarkMode(bool darkMode)
        {
            this.isDarkMode = darkMode;
            
            // 현재 페이지에도 다크모드 적용
            UpdateCurrentPageTheme();
        }

        /// <summary>
        /// 현재 페이지 반환
        /// </summary>
        public UserControl GetCurrentPage()
        {
            return currentPage;
        }

        /// <summary>
        /// OpticPage 표시
        /// </summary>
        public void ShowOpticPage()
        {
            // 기존 페이지 제거
            RemoveCurrentPage();

            // 메인 페이지 콘텐츠 숨기기
            HideMainPageContent();

            // OpticPage Lazy Loading
            if (_opticPage == null)
            {
                _opticPage = new OpticPage();
                System.Diagnostics.Debug.WriteLine("OpticPage 최초 생성됨");
            }

            // 다크모드 상태 전달
            _opticPage.SetDarkMode(isDarkMode);

            // 페이지 추가
            mainContent.Children.Add(_opticPage);
            currentPage = _opticPage;
            
            System.Diagnostics.Debug.WriteLine("OpticPage 표시됨 (재사용)");
        }

        /// <summary>
        /// OpticPage로 이동 (ShowOpticPage의 별칭)
        /// </summary>
        public void NavigateToOpticPage()
        {
            ShowOpticPage();
        }

        /// <summary>
        /// IPVSPage 표시
        /// </summary>
        public void ShowIPVSPage()
        {
            // 기존 페이지 제거
            RemoveCurrentPage();

            // 메인 페이지 콘텐츠 숨기기
            HideMainPageContent();

            // IPVSPage Lazy Loading
            if (_ipvsPage == null)
            {
                _ipvsPage = new IPVSPage();
                
                // IPVSPage의 뒤로가기 이벤트 처리 (1번만 등록)
                _ipvsPage.BackRequested += (s, e) => ShowMainPage();
                
                System.Diagnostics.Debug.WriteLine("IPVSPage 최초 생성됨");
            }

            // 다크모드 상태 전달
            _ipvsPage.SetDarkMode(isDarkMode);
            
            // 언어 상태 전달
            _ipvsPage.ApplyLanguage();

            // 페이지 추가
            mainContent.Children.Add(_ipvsPage);
            currentPage = _ipvsPage;
            
            System.Diagnostics.Debug.WriteLine("IPVSPage 표시됨 (재사용)");
        }

        /// <summary>
        /// ManualPage 표시
        /// </summary>
        public void ShowManualPage()
        {
            // 기존 페이지 제거
            RemoveCurrentPage();

            // 메인 페이지 콘텐츠 숨기기
            HideMainPageContent();

            // ManualPage Lazy Loading
            if (_manualPage == null)
            {
                _manualPage = new ManualPage();
                System.Diagnostics.Debug.WriteLine("ManualPage 최초 생성됨");
            }

            // 다크모드 상태 전달
            _manualPage.SetTheme(isDarkMode);

            // 페이지 추가
            mainContent.Children.Add(_manualPage);
            currentPage = _manualPage;
            
            System.Diagnostics.Debug.WriteLine("ManualPage 표시됨 (재사용)");
        }

        /// <summary>
        /// LUTPage 표시
        /// </summary>
        public void ShowLUTPage()
        {
            // 기존 페이지 제거
            RemoveCurrentPage();

            // 메인 페이지 콘텐츠 숨기기
            HideMainPageContent();

            // LUTPage Lazy Loading
            if (_lutPage == null)
            {
                _lutPage = new LUTPage();
                System.Diagnostics.Debug.WriteLine("LUTPage 최초 생성됨");
            }
            
            System.Diagnostics.Debug.WriteLine($"PageNavigationManager에서 LUT 페이지로 테마 전달: {(isDarkMode ? "다크" : "라이트")} 모드");
            _lutPage.SetTheme(isDarkMode);

            // 페이지 추가
            mainContent.Children.Add(_lutPage);
            currentPage = _lutPage;
            
            System.Diagnostics.Debug.WriteLine("LUT 페이지로 전환됨 (재사용)");
        }

        /// <summary>
        /// 메인 페이지 표시 (뒤로가기)
        /// </summary>
        public void ShowMainPage()
        {
            // 기존 페이지 제거
            RemoveCurrentPage();

            // 메인 페이지 콘텐츠 다시 표시
            if (mainPageContent != null)
            {
                mainPageContent.Visibility = Visibility.Visible;
            }
            
            currentPage = null;
        }

        /// <summary>
        /// 현재 페이지 제거
        /// </summary>
        private void RemoveCurrentPage()
        {
            if (currentPage != null && mainContent != null)
            {
                mainContent.Children.Remove(currentPage);
            }
        }

        /// <summary>
        /// 메인 페이지 콘텐츠 숨기기
        /// </summary>
        private void HideMainPageContent()
        {
            if (mainPageContent != null)
            {
                mainPageContent.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 현재 페이지에 다크모드 적용
        /// </summary>
        private void UpdateCurrentPageTheme()
        {
            if (currentPage is OpticPage opticPage)
            {
                opticPage.SetDarkMode(isDarkMode);
            }
            else if (currentPage is IPVSPage ipvsPage)
            {
                ipvsPage.SetDarkMode(isDarkMode);
            }
            else if (currentPage is ManualPage manualPage)
            {
                manualPage.SetTheme(isDarkMode);
            }
            else if (currentPage is LUTPage lutPage)
            {
                lutPage.SetTheme(isDarkMode);
            }
        }

        /// <summary>
        /// 현재 페이지에 언어 적용
        /// </summary>
        public void ApplyLanguageToCurrentPage()
        {
            if (currentPage is OpticPage opticPage)
            {
                opticPage.ApplyLanguage();
            }
            else if (currentPage is IPVSPage ipvsPage)
            {
                ipvsPage.ApplyLanguage();
            }
        }
    }
}




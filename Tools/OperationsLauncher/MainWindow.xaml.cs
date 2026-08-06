using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace TinyHero.OperationsLauncher;

public partial class MainWindow : Window
{
    private enum eServiceOperation
    {
        NONE,
        START,
        STOP
    }

    private enum eManagedService
    {
        NONE,
        JENKINS,
        CONTENT,
        PORTAL
    }

    private const string JenkinsUrl = "http://127.0.0.1:8081";
    private const string ContentUrl = "http://127.0.0.1:8082";
    private const string PortalUrl = "http://127.0.0.1:8090";
    private const double MinimumPersistedWindowWidth = 440.0;
    private const double MinimumPersistedWindowHeight = 620.0;
    private const double MaximumPersistedWindowWidth = 2400.0;
    private const double MaximumPersistedWindowHeight = 1600.0;

    private readonly DispatcherTimer statusTimer;
    private readonly string projectRootPath;
    private bool isRefreshingStatus;
    private eServiceOperation pendingServiceOperation;
    private eManagedService pendingManagedService;

    private sealed class CWindowLayoutData
    {
        public double left { get; set; }
        public double top { get; set; }
        public double width { get; set; }
        public double height { get; set; }
        public bool isMaximized { get; set; }
    }

    public MainWindow()
    {
        InitializeComponent();

        projectRootPath = FindProjectRoot();
        statusTimer = new DispatcherTimer();
        statusTimer.Interval = TimeSpan.FromSeconds( 5 );
        statusTimer.Tick += async ( _sender, _eventArgs ) => await RefreshStatusAsync();
        statusTimer.Start();
        SourceInitialized += HandleSourceInitialized;
        Loaded += HandleLoaded;
        Closing += HandleClosing;
    }

    private async void HandleLoaded( object? _sender, RoutedEventArgs _eventArgs )
    {
        await RefreshStatusAsync();
    }

    private void HandleSourceInitialized( object? _sender, EventArgs _eventArgs )
    {
        RestoreWindowLayout();
    }

    private void HandleClosing( object? _sender, System.ComponentModel.CancelEventArgs _eventArgs )
    {
        SaveWindowLayout();
    }

    private async void OnRefreshClicked( object _sender, RoutedEventArgs _eventArgs )
    {
        await RefreshStatusAsync();
    }

    private async void OnStartClicked( object _sender, RoutedEventArgs _eventArgs )
    {
        pendingServiceOperation = eServiceOperation.START;
        pendingManagedService = eManagedService.NONE;
        messageText.Text = "서비스 시작 요청을 전달했습니다. 서비스 상태를 확인하는 중입니다.";
        await RunServiceScriptAsync( eManagedService.JENKINS, eServiceOperation.START );
        await RunServiceScriptAsync( eManagedService.CONTENT, eServiceOperation.START );
        await RunServiceScriptAsync( eManagedService.PORTAL, eServiceOperation.START );
    }

    private async void OnStopClicked( object _sender, RoutedEventArgs _eventArgs )
    {
        MessageBoxResult result = MessageBox.Show( "Jenkins, 콘텐츠 서버, 운영 포털을 종료할까요?", "TinyHero 운영 센터", MessageBoxButton.YesNo, MessageBoxImage.Warning );

        if ( result != MessageBoxResult.Yes )
        {
            return;
        }

        pendingServiceOperation = eServiceOperation.STOP;
        pendingManagedService = eManagedService.NONE;
        messageText.Text = "서비스 종료 요청을 전달했습니다. 서비스 상태를 확인하는 중입니다.";
        await RunServiceScriptAsync( eManagedService.PORTAL, eServiceOperation.STOP );
        await RunServiceScriptAsync( eManagedService.CONTENT, eServiceOperation.STOP );
        await RunServiceScriptAsync( eManagedService.JENKINS, eServiceOperation.STOP );
    }

    private async void OnServiceToggleClicked( object _sender, RoutedEventArgs _eventArgs )
    {
        System.Windows.Controls.Button? toggleButton = _sender as System.Windows.Controls.Button;
        string serviceName = toggleButton?.Tag?.ToString() ?? string.Empty;
        bool isServiceValid = Enum.TryParse( serviceName, true, out eManagedService managedService );

        if ( isServiceValid == false || managedService == eManagedService.NONE )
        {
            messageText.Text = "관리할 서비스를 확인할 수 없습니다.";
            return;
        }

        bool isOnline = await IsPortOpenAsync( GetServicePort( managedService ) );
        eServiceOperation operation = isOnline ? eServiceOperation.STOP : eServiceOperation.START;

        if ( operation == eServiceOperation.STOP )
        {
            string warningMessage = managedService == eManagedService.JENKINS
                ? "Jenkins를 종료할까요? 진행 중인 빌드가 있다면 즉시 중단됩니다."
                : $"{GetServiceDisplayName( managedService )} 서비스를 종료할까요?";
            MessageBoxResult result = MessageBox.Show( warningMessage, "TinyHero 운영 센터", MessageBoxButton.YesNo, MessageBoxImage.Warning );

            if ( result != MessageBoxResult.Yes )
            {
                return;
            }
        }

        pendingServiceOperation = operation;
        pendingManagedService = managedService;
        string operationName = operation == eServiceOperation.START ? "시작" : "종료";
        messageText.Text = $"{GetServiceDisplayName( managedService )} {operationName} 요청을 전달했습니다.";
        await RunServiceScriptAsync( managedService, operation );
    }

    private void OnOpenPortalClicked( object _sender, RoutedEventArgs _eventArgs )
    {
        OpenUrl( PortalUrl );
    }

    private void OnOpenJenkinsClicked( object _sender, RoutedEventArgs _eventArgs )
    {
        OpenUrl( JenkinsUrl );
    }

    private async Task RunScriptAsync( string _scriptPath, string _scriptArguments )
    {
        if ( File.Exists( _scriptPath ) == false )
        {
            pendingServiceOperation = eServiceOperation.NONE;
            messageText.Text = $"실행 스크립트를 찾을 수 없습니다: {_scriptPath}";
            return;
        }

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "powershell.exe";
            startInfo.Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{_scriptPath}\" {_scriptArguments}";
            startInfo.WorkingDirectory = projectRootPath;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            Process? process = Process.Start( startInfo );

            if ( process == null )
            {
                throw new InvalidOperationException( "실행 프로세스를 시작하지 못했습니다." );
            }

            await Task.Delay( 800 );
            await RefreshStatusAsync();
        }
        catch ( Exception exception )
        {
            pendingServiceOperation = eServiceOperation.NONE;
            messageText.Text = $"실행 중 오류가 발생했습니다: {exception.Message}";
        }
    }

    private async Task RunServiceScriptAsync( eManagedService _service, eServiceOperation _operation )
    {
        string scriptPath = Path.Combine( projectRootPath, "Tools", "OperationsPortal", "Manage-TinyHeroOperationsService.ps1" );
        string operationName = _operation == eServiceOperation.START ? "START" : "STOP";
        string scriptArguments = $"-Service {_service} -Action {operationName}";
        await RunScriptAsync( scriptPath, scriptArguments );
    }

    private async Task RefreshStatusAsync()
    {
        if ( isRefreshingStatus )
        {
            return;
        }

        isRefreshingStatus = true;
        StartRefreshAnimation();

        try
        {
            bool jenkinsOnline = await IsPortOpenAsync( 8081 );
            bool contentOnline = await IsPortOpenAsync( 8082 );
            bool portalOnline = await IsPortOpenAsync( 8090 );

            SetServiceStatus( jenkinsIndicator, jenkinsStatusText, jenkinsOnline );
            SetServiceStatus( contentIndicator, contentStatusText, contentOnline );
            SetServiceStatus( portalIndicator, portalStatusText, portalOnline );
            SetServiceToggleButton( jenkinsToggleButton, jenkinsOnline );
            SetServiceToggleButton( contentToggleButton, contentOnline );
            SetServiceToggleButton( portalToggleButton, portalOnline );

            bool allOnline = jenkinsOnline && contentOnline && portalOnline;
            overallStatusBadge.Background = new SolidColorBrush( ColorConverter.ConvertFromString( allOnline ? "#DCFCE7" : "#FEE4E2" ) as Color? ?? Colors.Transparent );
            overallStatusText.Foreground = new SolidColorBrush( ColorConverter.ConvertFromString( allOnline ? "#15803D" : "#B42318" ) as Color? ?? Colors.Transparent );
            overallStatusText.Text = allOnline ? "ALL ONLINE" : "OFFLINE";
            lastUpdatedText.Text = $"마지막 새로고침: {DateTime.Now:HH:mm:ss}";
            UpdateOperationMessage( jenkinsOnline, contentOnline, portalOnline );
        }
        finally
        {
            StopRefreshAnimation();
            isRefreshingStatus = false;
        }
    }

    private void UpdateOperationMessage( bool _jenkinsOnline, bool _contentOnline, bool _portalOnline )
    {
        if ( pendingManagedService != eManagedService.NONE )
        {
            bool isManagedServiceOnline = GetServiceOnlineState( pendingManagedService, _jenkinsOnline, _contentOnline, _portalOnline );
            bool isOperationComplete = pendingServiceOperation == eServiceOperation.START
                ? isManagedServiceOnline
                : isManagedServiceOnline == false;

            if ( isOperationComplete )
            {
                string operationName = pendingServiceOperation == eServiceOperation.START ? "시작" : "종료";
                messageText.Text = $"{GetServiceDisplayName( pendingManagedService )} {operationName}이 완료되었습니다.";
                pendingServiceOperation = eServiceOperation.NONE;
                pendingManagedService = eManagedService.NONE;
            }

            return;
        }

        bool allOnline = _jenkinsOnline && _contentOnline && _portalOnline;
        bool allOffline = _jenkinsOnline == false && _contentOnline == false && _portalOnline == false;

        if ( pendingServiceOperation == eServiceOperation.START )
        {
            if ( allOnline )
            {
                pendingServiceOperation = eServiceOperation.NONE;
                messageText.Text = "모든 로컬 서비스가 정상적으로 시작되었습니다.";
                return;
            }

            messageText.Text = "서비스를 시작하는 중입니다. Jenkins 준비에는 잠시 시간이 걸릴 수 있습니다.";
            return;
        }

        if ( pendingServiceOperation == eServiceOperation.STOP )
        {
            if ( allOffline )
            {
                pendingServiceOperation = eServiceOperation.NONE;
                messageText.Text = "모든 로컬 서비스가 안전하게 종료되었습니다.";
                return;
            }

            messageText.Text = "서비스를 종료하는 중입니다. 포트가 모두 닫힐 때까지 확인합니다.";
        }
    }

    private void StartRefreshAnimation()
    {
        refreshButton.IsEnabled = false;
        refreshButtonText.Text = "확인 중";

        DoubleAnimation rotationAnimation = new DoubleAnimation();
        rotationAnimation.From = 0;
        rotationAnimation.To = 360;
        rotationAnimation.Duration = TimeSpan.FromMilliseconds( 700 );
        rotationAnimation.RepeatBehavior = RepeatBehavior.Forever;

        RotateTransform rotationTransform = (RotateTransform)refreshIcon.RenderTransform;
        rotationTransform.BeginAnimation( RotateTransform.AngleProperty, rotationAnimation );
    }

    private void StopRefreshAnimation()
    {
        RotateTransform rotationTransform = (RotateTransform)refreshIcon.RenderTransform;
        rotationTransform.BeginAnimation( RotateTransform.AngleProperty, null );
        rotationTransform.Angle = 0;
        refreshButtonText.Text = "새로고침";
        refreshButton.IsEnabled = true;
    }

    private static async Task<bool> IsPortOpenAsync( int _port )
    {
        using TcpClient client = new TcpClient();
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource( TimeSpan.FromMilliseconds( 700 ) );

        try
        {
            await client.ConnectAsync( "127.0.0.1", _port, cancellationTokenSource.Token );
            return true;
        }
        catch ( SocketException )
        {
            return false;
        }
        catch ( OperationCanceledException )
        {
            return false;
        }
    }

    private static void SetServiceStatus( System.Windows.Shapes.Ellipse _indicator, System.Windows.Controls.TextBlock _statusText, bool _isOnline )
    {
        _indicator.Fill = new SolidColorBrush( ColorConverter.ConvertFromString( _isOnline ? "#22C55E" : "#98A2B3" ) as Color? ?? Colors.Transparent );
        _statusText.Text = _isOnline ? "ON" : "OFF";
        _statusText.Foreground = new SolidColorBrush( ColorConverter.ConvertFromString( _isOnline ? "#15803D" : "#667085" ) as Color? ?? Colors.Transparent );
    }

    private static void SetServiceToggleButton( System.Windows.Controls.Button _button, bool _isOnline )
    {
        _button.Content = _isOnline ? "끄기" : "켜기";
        _button.Background = new SolidColorBrush( ColorConverter.ConvertFromString( _isOnline ? "#FEE4E2" : "#DCFCE7" ) as Color? ?? Colors.Transparent );
        _button.Foreground = new SolidColorBrush( ColorConverter.ConvertFromString( _isOnline ? "#B42318" : "#15803D" ) as Color? ?? Colors.Transparent );
        _button.IsEnabled = true;
    }

    private static int GetServicePort( eManagedService _service )
    {
        int port = _service switch
        {
            eManagedService.JENKINS => 8081,
            eManagedService.CONTENT => 8082,
            eManagedService.PORTAL => 8090,
            _ => 0
        };
        return port;
    }

    private static string GetServiceDisplayName( eManagedService _service )
    {
        string displayName = _service switch
        {
            eManagedService.JENKINS => "Jenkins",
            eManagedService.CONTENT => "콘텐츠 서버",
            eManagedService.PORTAL => "운영 포털",
            _ => "서비스"
        };
        return displayName;
    }

    private static bool GetServiceOnlineState( eManagedService _service, bool _jenkinsOnline, bool _contentOnline, bool _portalOnline )
    {
        bool isOnline = _service switch
        {
            eManagedService.JENKINS => _jenkinsOnline,
            eManagedService.CONTENT => _contentOnline,
            eManagedService.PORTAL => _portalOnline,
            _ => false
        };
        return isOnline;
    }

    private static void OpenUrl( string _url )
    {
        ProcessStartInfo startInfo = new ProcessStartInfo( _url );
        startInfo.UseShellExecute = true;
        Process.Start( startInfo );
    }

    private void RestoreWindowLayout()
    {
        string layoutFilePath = GetWindowLayoutFilePath();

        if ( File.Exists( layoutFilePath ) == false )
        {
            return;
        }

        try
        {
            string layoutJsonText = File.ReadAllText( layoutFilePath );
            CWindowLayoutData? layoutData = JsonSerializer.Deserialize<CWindowLayoutData>( layoutJsonText );

            if ( IsWindowLayoutValid( layoutData ) == false || layoutData == null )
            {
                return;
            }

            Width = layoutData.width;
            Height = layoutData.height;
            Left = layoutData.left;
            Top = layoutData.top;

            if ( layoutData.isMaximized )
            {
                WindowState = WindowState.Maximized;
            }
        }
        catch ( Exception exception )
        {
            Debug.WriteLine( $"Window layout restore failed: {exception.Message}" );
        }
    }

    private void SaveWindowLayout()
    {
        try
        {
            WindowState savedWindowState = WindowState;

            if ( savedWindowState == WindowState.Minimized )
            {
                return;
            }

            CWindowLayoutData layoutData = new CWindowLayoutData();
            layoutData.left = RestoreBounds.Left;
            layoutData.top = RestoreBounds.Top;
            layoutData.width = RestoreBounds.Width;
            layoutData.height = RestoreBounds.Height;
            layoutData.isMaximized = savedWindowState == WindowState.Maximized;

            if ( IsWindowLayoutValid( layoutData ) == false )
            {
                return;
            }

            string layoutFilePath = GetWindowLayoutFilePath();
            string? layoutDirectoryPath = Path.GetDirectoryName( layoutFilePath );

            if ( string.IsNullOrWhiteSpace( layoutDirectoryPath ) == false )
            {
                Directory.CreateDirectory( layoutDirectoryPath );
            }

            string layoutJsonText = JsonSerializer.Serialize( layoutData );
            File.WriteAllText( layoutFilePath, layoutJsonText );
        }
        catch ( Exception exception )
        {
            Debug.WriteLine( $"Window layout save failed: {exception.Message}" );
        }
    }

    private static string GetWindowLayoutFilePath()
    {
        string localApplicationDataPath = Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData );
        string layoutDirectoryPath = Path.Combine( localApplicationDataPath, "TinyHero", "OperationsLauncher" );
        string layoutFilePath = Path.Combine( layoutDirectoryPath, "window-layout.json" );
        return layoutFilePath;
    }

    private static bool IsWindowLayoutValid( CWindowLayoutData? _layoutData )
    {
        if ( _layoutData == null )
        {
            return false;
        }

        bool hasValidWidth = _layoutData.width >= MinimumPersistedWindowWidth && _layoutData.width <= MaximumPersistedWindowWidth;
        bool hasValidHeight = _layoutData.height >= MinimumPersistedWindowHeight && _layoutData.height <= MaximumPersistedWindowHeight;
        bool hasValidLeft = double.IsNaN( _layoutData.left ) == false && double.IsInfinity( _layoutData.left ) == false;
        bool hasValidTop = double.IsNaN( _layoutData.top ) == false && double.IsInfinity( _layoutData.top ) == false;
        bool result = hasValidWidth && hasValidHeight && hasValidLeft && hasValidTop;
        return result;
    }

    private static string FindProjectRoot()
    {
        DirectoryInfo? directory = new DirectoryInfo( AppContext.BaseDirectory );

        while ( directory != null )
        {
            string scriptPath = Path.Combine( directory.FullName, "Tools", "OperationsPortal", "Start-TinyHeroOperationsPortal-Desktop.ps1" );

            if ( File.Exists( scriptPath ) )
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException( "TinyHero 프로젝트 폴더를 찾지 못했습니다." );
    }
}

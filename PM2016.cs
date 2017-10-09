/*  
 *  PM2016.cs   유진석
 *  
 *  Process Manager 2016의 주 소스 코드 입니다.
 *  (최종수정 2017-10-09) 
 */

using System;
using System.Drawing;
using System.Windows.Forms;

using System.Diagnostics;   //Class Process
using System.Threading;     //Threading

namespace PM2016
{
    public partial class PM2016 : Form
    {
        private const int listUpdatePeriod = 10000;     //목록갱신 주기(ms)
        private const int cpuUsageUpdatePeriod = 1000;  //CPU 사용률 갱신 주기(ms)

        private Thread listThread;                      //프로세스 목록 스레드
        private Thread cpuThread;                       //CPU 사용률 스레드

        private int procNum = 0;                        //프로세스 갯수
        private long usingMem = 0;                      //총 메모리 사용량

        //목록 갱신용 Delegate
        private delegate void UpdateDelegate();
        private UpdateDelegate updateDelg;

        //[생성자]
        public PM2016()
        {
            InitializeComponent();      //컴포넌트 초기화 PM2016.Designer.cs에 정의
        }

        //[생성 후 프로그램 로드]
        private void PM2016_Load(object sender, EventArgs e)
        {
            //Delegate 할당
            updateDelg = new UpdateDelegate(UpdateProcessView);

            //Thread 초기화
            listThread = new Thread(UpdateThread);
            cpuThread = new Thread(UpdateCpuUsageThread);

            //Thread 시작
            listThread.Start();
            cpuThread.Start();
        }

        //[프로세스 목록 갱신 Thread]
        private void UpdateThread()
        {
            try
            { 
                //프로그램 종료시까지 반복
                while (true)
                {
                    //10초마다 프로세스 목록 갱신
                    Invoke(updateDelg);
                    Thread.Sleep(listUpdatePeriod); 
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        //[프로세스 목록 출력]
        private void UpdateProcessView()
        {
            PerformanceCounter memCounter;      //가용 메모리 PerformanceCounter(MB)
            long availMem;                      //가용 메모리를 담을 변수(64bit/Byte)
            double memUsage;                    //메모리 사용률을 담을 변수
            long tempMem;                       //각 프로세스별 메모리를 더할 변수

            try
            {
                this.listView.Items.Clear();    //리스트 뷰 '항목' 초기화
                procNum = 0;                    //프로세스 갯수 초기화
                usingMem = 0;                   //총 메모리 사용량 초기화

                //실행중인 프로세스들을 읽어들이면서 목록에 하나씩 출력
                foreach (Process proc in Process.GetProcesses())
                {
                    tempMem = proc.WorkingSet64;//실제 메모리의 양을 반환(64bit/Byte)
                    usingMem += tempMem;        //총 메모리 사용량 가산

                    //리스트 뷰의 각 항목 (각 프로세스에 대응 됨)
                    var strArray = new string[]
                    {
                        proc.ProcessName.ToString(),//프로세스이름
                        proc.Id.ToString(),         //프로세스 PID
                        MemoryString(tempMem),      //메모리 사용량 -- MemoryString함수로 텍스트 형식 변환
                    };

                    //리스트뷰에 항목 추가
                    ListViewItem listViewItem = new ListViewItem(strArray);
                    this.listView.Items.Add(listViewItem);

                    //프로세스 총 갯수 가산
                    procNum++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            //프로세스 갯수 출력
            this.toolProcess.Text = Properties.Resources.textProcess + " " + procNum.ToString() + " " + Properties.Resources.textGae;
            
            //메모리 사용률 출력
            memCounter = new PerformanceCounter("Memory", "Available MBytes");
            availMem =  Convert.ToInt64(memCounter.NextValue() * 1024 * 1024); //가용 메모리를 Byte로 변환
            memUsage = ((double)usingMem / (double)(usingMem + availMem))*100;
            this.toolMem.Text = Properties.Resources.textMemUsage + " : " + Convert.ToString(String.Format("{0:f2}", memUsage) + "%");
        }


        //[Byte형태의 메모리 사용량을 읽어 MB형태의 문자열로 변환.]
        private string MemoryString(long MemoryNum)
        {
            Double memNum;
            memNum = Convert.ToDouble(MemoryNum);
            memNum = memNum / (1024 * 1024);
            return String.Format("{0:N}", memNum) + "MB";
        }

        //[CPU 사용률 Thread]
        public void UpdateCpuUsageThread()
        {
            //<MSDN>카운터의 계산된 된 값 두 개의 카운터 읽기에 의존 하는 경우
            //첫 번째 읽기 작업이 0.0을 반환 합니다.
            //한 호출 사이의 권장된 지연 시간은 NextValue 메서드 1초 입니다.</MSDN>

            PerformanceCounter cpuCounter;
            while (true)
            {
                //현재 CPU 사용률을 받아옴.
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpuCounter.NextValue();
                Thread.Sleep(cpuUsageUpdatePeriod);
                this.toolCPU.Text = Properties.Resources.textCpuUsage + " : " + Convert.ToString(String.Format("{0:f2}", cpuCounter.NextValue()) + "%");
            }
        }

        //[BtnKill 클릭 이벤트]
        private void BtnKill_Click(object sender, EventArgs e)
        {
            //프로세스 종료 함수 실행
            ProcessKill();
        }

        //[프로세스 종료] (BtnKill 버튼 클릭시 실행)
        private void ProcessKill()
        {
            try
            {   //선택된 리스트 항목의 PID를 구함
                int PID = Convert.ToInt32(this.listView.SelectedItems[0].SubItems[1].Text);
                
                //PID로 프로세스 탐색
                Process targetProcess = Process.GetProcessById(PID);

                if (!(targetProcess == null))
                {
                    //선택한 프로세스가 존재할 경우, 종료할지 물어보는 메시지박스를 출력
                    DialogResult message =
                        MessageBox.Show
                        (
                            this.listView.SelectedItems[0].SubItems[0].Text +
                            Properties.Resources.textProcessKill, Properties.Resources.textWarning,
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning
                        );
                    ////사용자가 종료에 대해 확인했을 때, 실제로 프로세스를 종료하고, 목록을 갱신
                    if (message == DialogResult.Yes)
                    {
                        targetProcess.Kill();
                        UpdateProcessView();

                    }
                }
                else
                {
                    //선택한 프로세스가 존재하지 않을경우 에러메시지박스를 출력
                    MessageBox.Show
                    (
                        this.listView.SelectedItems[0].SubItems[0].Text +
                        Properties.Resources.textProcessKill, Properties.Resources.textWarning,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                UpdateProcessView();
            }
        }

        //[ListView 마우스 클릭 이벤트]
        private void ListView_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                //오른쪽 클릭일 경우
                if (e.Button.Equals(MouseButtons.Right))
                {
                    //프로세스 선호도/우선순위 설정 메뉴 출력
                    ProcessSetMenu(e);
                }
            }
            catch (Exception ex)
            {
                //시스템 프로세스 권한문제로 예외 발생 가능
                Console.WriteLine(ex.Message);
            }
        }

        //[프로세스 선호도/우선순위 설정 메뉴] (ListView 항목 클릭시 실행)
        private void ProcessSetMenu(MouseEventArgs e)
        {
            //선택된 리스트 항목의 PID를 구함
            int PID = Convert.ToInt32(this.listView.SelectedItems[0].SubItems[1].Text);

            //PID로 프로세스 탐색
            Process targetProcess = Process.GetProcessById(PID);

            //메뉴에 들어갈 아이템 할당
            MenuItem menuAffinity = new MenuItem(Properties.Resources.textSetAffinity);  //선호도 설정
            MenuItem menuPriority = new MenuItem(Properties.Resources.textSetPriority);  //우선순위 설정

            //선호도 설정 클릭 이벤트
            menuAffinity.Click += (senders, es) =>
            {
                //선호도 설정 Form을  (생성할 때, 해당 프로세스의 현재 선호도를 전달)
                AffinityForm aForm = new AffinityForm(targetProcess.ProcessorAffinity);

                //Form에서 설정한 내용을 반영.
                if (aForm.ShowDialog() == DialogResult.OK)
                {
                    targetProcess.ProcessorAffinity = aForm.AffinityNum;
                }
            };

            //우선순위 설정 클릭 이벤트
            {
                //우선순위에 따른 MenuItem 할당
                MenuItem prReti = new MenuItem(Properties.Resources.textPriorityRealtime);
                MenuItem prHigh = new MenuItem(Properties.Resources.textPriorityHigh);
                MenuItem prAbno = new MenuItem(Properties.Resources.textPriorityAbovenormal);
                MenuItem prNorm = new MenuItem(Properties.Resources.textPriorityNormal);
                MenuItem prBeno = new MenuItem(Properties.Resources.textPriorityBelownormal);
                MenuItem prIdle = new MenuItem(Properties.Resources.textPriorityIdle);
                //하위메뉴들 추가
                menuPriority.MenuItems.Add(prReti);
                menuPriority.MenuItems.Add(prHigh);
                menuPriority.MenuItems.Add(prAbno);
                menuPriority.MenuItems.Add(prNorm);
                menuPriority.MenuItems.Add(prBeno);
                menuPriority.MenuItems.Add(prIdle);
                //라디오버튼 활성화
                prReti.RadioCheck = true;
                prHigh.RadioCheck = true;
                prAbno.RadioCheck = true;
                prNorm.RadioCheck = true;
                prBeno.RadioCheck = true;
                prIdle.RadioCheck = true;

                //우선순위 설정
                {
                    //현재 우선순위에 따라 라디오버튼을 체크
                    switch (targetProcess.PriorityClass)
                    {
                        case ProcessPriorityClass.RealTime:
                            {
                                prReti.Checked = true;
                                break;
                            }
                        case ProcessPriorityClass.High:
                            {
                                prHigh.Checked = true;
                                break;
                            }
                        case ProcessPriorityClass.AboveNormal:
                            {
                                prAbno.Checked = true;
                                break;
                            }
                        case ProcessPriorityClass.Normal:
                            {
                                prNorm.Checked = true;
                                break;
                            }
                        case ProcessPriorityClass.BelowNormal:
                            {
                                prBeno.Checked = true;
                                break;
                            }
                        case ProcessPriorityClass.Idle:
                            {
                                prIdle.Checked = true;
                                break;
                            }
                    }
                    //라디오버튼을 선택했을 때, 우선순위를 변경
                    prReti.Click += (senders, es) =>
                    {
                        targetProcess.PriorityClass = ProcessPriorityClass.RealTime;
                    };
                    prHigh.Click += (senders, es) =>
                    {
                        targetProcess.PriorityClass = ProcessPriorityClass.High;
                    };
                    prAbno.Click += (senders, es) =>
                    {
                        targetProcess.PriorityClass = ProcessPriorityClass.AboveNormal;
                    };
                    prNorm.Click += (senders, es) =>
                    {
                        targetProcess.PriorityClass = ProcessPriorityClass.Normal;
                    };
                    prBeno.Click += (senders, es) =>
                    {
                        targetProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
                    };
                    prIdle.Click += (senders, es) =>
                    {
                        targetProcess.PriorityClass = ProcessPriorityClass.Idle;
                    };
                }
            }

            //메뉴를 할당
            ContextMenu rightMenu = new ContextMenu();

            //메뉴에 메뉴 아이템 등록
            rightMenu.MenuItems.Add(menuAffinity);
            rightMenu.MenuItems.Add(menuPriority);

            //현재 마우스가 위치한 장소에 메뉴 출력
            rightMenu.Show(this.listView, new Point(e.X, e.Y));
        }
    }
}

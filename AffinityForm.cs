/*  
 *  AffinityForm.cs   유진석
 *  
 *  Process Manager 2016의 프로세스 선호도 설정 부 입니다.
 *  (최종수정 2017-10-09) 
 */

using System;
using System.Windows.Forms;

namespace PM2016
{
    public partial class AffinityForm : Form
    {
        private int MAX_CORE = 4;   //쿼드코어 CPU 기준 코어의 수 (본래는 구동 중인 PC에 맞게 설정해야 함)

        public IntPtr AffinityNum
        {
            get;
            set;
        }

        //[생성자]
        public AffinityForm(IntPtr AffNum)
        {
            this.AffinityNum = AffNum;
            InitializeComponent();
        }

        //[폼 로드시 체크박스 초기 값 설정]
        private void AffinityForm_Load(object sender, EventArgs e)
        {
            //선호도 리스트 생성
            this.checkedListBox1.Items.Add("<"+Properties.Resources.textWholeProcessor+">");
            for(int i = 0; i < MAX_CORE; i ++)
            {
                this.checkedListBox1.Items.Add(Properties.Resources.textCPU + " " + i.ToString());
            }
            int count = 0; //체크된 항목 갯수 초기화

            {
                byte affinityByte = (byte)AffinityNum;  //비트연산을 위해 바이트로 형변환
                byte shifter = 0b0001;

                //각각의 CPU에 대해 선호도가 설정되어있을 경우, 체크박스에 체크
                for(int i = 0; i < MAX_CORE; i ++)
                {
                    if((affinityByte & shifter) == shifter)
                    {
                        this.checkedListBox1.SetItemChecked(i + 1, true);
                        count++;
                    }
                    shifter <<= 1;
                }
                //모든 CPU에 대해 선호도가 설정되어있을 경우, <모든 프로세서> 항목에 체크
                if(count == MAX_CORE)
                {
                    this.checkedListBox1.SetItemChecked(0, true);
                }
            }

        }

        //[체크박스 항목 변경시 호출]
        private void CheckedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = checkedListBox1.SelectedIndex; //변경된 항목의 인덱스
            
            if(index == 0)
            {//<모든 프로세서> 항목을 변경했을 때, 모든 항목을 체크/체크 해제
                if(checkedListBox1.GetItemChecked(0))
                {
                    for(int i = 0; i < MAX_CORE; i ++)
                    {
                        this.checkedListBox1.SetItemChecked(i+1, true);
                    }
                }
                else
                {
                    for (int i = 0; i < MAX_CORE; i++)
                    {
                        this.checkedListBox1.SetItemChecked(i + 1, false);
                    }
                }
            }

            if(checkedListBox1.GetItemChecked(1) && checkedListBox1.GetItemChecked(2) && checkedListBox1.GetItemChecked(3) && checkedListBox1.GetItemChecked(4))
            {//모든 CPU에 체크되었을 떄 <모든 프로세서> 항목을 체크
                this.checkedListBox1.SetItemChecked(0, true);
            }
            else
            {//적어도 하나의 CPU가 체크되지 않았을 때, <모든 프로세서> 항목의 체크를 해제
                this.checkedListBox1.SetItemChecked(0, false);
            }

            if (checkedListBox1.GetItemChecked(1) || checkedListBox1.GetItemChecked(2) || checkedListBox1.GetItemChecked(3) || checkedListBox1.GetItemChecked(4))
            {//적어도 하나의 CPU가 체크되었을 때만, 확인 버튼 활성화
                button1.Enabled = true;
            }
            else
            {
                button1.Enabled = false;
            }
        }

        //[확인 버튼 클릭 시, 체크박스의 상태에 따라 선호도 적용]
        private void Button1_Click(object sender, EventArgs e)
        {
            AffinityNum = (IntPtr)0;
            if (checkedListBox1.GetItemChecked(0))
            {//모든 CPU에 체크되어 있을 때, 값은 쿼드코어 기준 0b1111
                AffinityNum = (IntPtr)(2 << MAX_CORE - 1);
            }
            else
            {//모든 CPU에 체크되어있지 않을 경우, 체크된 CPU 각각의 플래그를 가산
                int tempAff = 0;

                for(int i = 0; i < MAX_CORE; i ++)
                {
                    if (checkedListBox1.GetItemChecked(i + 1))
                    {
                        tempAff += (1 << i);
                    }
                }
                AffinityNum = (IntPtr)tempAff;
            }
            this.Close();
        }

        //[창 닫기]
        private void Button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}

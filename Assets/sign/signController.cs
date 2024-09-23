using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;


class ThreadSafeBoolean
{
    private int _flag;

    public ThreadSafeBoolean(bool initialValue = false)
    {
        _flag = initialValue ? 1 : 0;
    }

    public bool Value
    {
        get => Interlocked.CompareExchange(ref _flag, 0, 0) == 1;
        set => Interlocked.Exchange(ref _flag, value ? 1 : 0);
    }
}


public class signController : MonoBehaviour
{
    private Animator anim;
    private static BlockingCollection<string> messageQueue = new BlockingCollection<string>();
    private ThreadSafeBoolean isAnimating = new ThreadSafeBoolean(false);

    Dictionary<KeyCode, string> mapAlphabetical = new Dictionary<KeyCode, string>
    {
        { KeyCode.R, "��" },
        { KeyCode.S, "��" },
        { KeyCode.E, "��" },
        { KeyCode.F, "��" },
        { KeyCode.A, "��" },
        { KeyCode.Q, "��" },
        { KeyCode.T, "��" },
        { KeyCode.D, "��" },
        { KeyCode.W, "��" },
        { KeyCode.C, "��" },
        { KeyCode.Z, "��" },
        { KeyCode.X, "��" },
        { KeyCode.V, "��" },
        { KeyCode.G, "��" },
        { KeyCode.K, "��" },
        { KeyCode.I, "��" },
        { KeyCode.J, "��" },
        { KeyCode.U, "��" },
        { KeyCode.H, "��" },
        { KeyCode.Y, "��" },
        { KeyCode.N, "��" },
        { KeyCode.B, "��" },
        { KeyCode.M, "��" },
        { KeyCode.L, "��" },
        { KeyCode.O, "��" },
        { KeyCode.P, "��" },
    };

    static Dictionary<string, string> animMap = new Dictionary<string, string>();

    static void GetAnimMap()
    {
        string filePath = "./Assets/sign/sign.csv"; // CSV ���� ���
        Debug.Log(filePath);
        try
        {
            using (var reader = new StreamReader(filePath))
            {   
                reader.ReadLine();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    if (values.Length == 2)
                    {
                        string key = values[0].Trim();
                        string value = values[1].Trim();

                        animMap[key] = value;
                    }
                }
            }

            foreach (string key in animMap.Keys)
                Debug.Log($"{key} : {animMap[key]}");
        }
        catch (Exception ex)
        {
            Debug.Log($"Error: {ex.Message}");
        }
    }

    // Start is called before the first frame update
    void Start()
    {   
        GetAnimMap();
        anim = gameObject.GetComponent<Animator>();

        // ������ ��ü ����
        Thread socketThread = new Thread(new ThreadStart(Execute));

        // �����带 ���� ������� ����
        socketThread.IsBackground = true;

        // ������ ����
        socketThread.Start();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isAnimating.Value)
        {
            /*
            foreach (var (key, action) in mapAlphabetical)
            {
                if (Input.GetKeyDown(key))
                {
                    isAnimating.Value = true;
                    StartCoroutine(PlayAnimationAndWait(action));
                    return;
                }
            }
            */
            if (messageQueue.Count > 0)
            {
                string action = messageQueue.Take();
                if (animMap.ContainsKey(action))
                {
                    isAnimating.Value = true;
                    StartCoroutine(PlayAnimationAndWait(animMap[action]));
                }
            }
            else
            {
                isAnimating.Value = true;
                StartCoroutine(PlayAnimationAndWait("idle"));
            }
        }
    }

    private IEnumerator PlayAnimationAndWait(string animName)
    {
        // �ִϸ��̼��� ũ�ν����̵�� ����
        anim.CrossFade(animName, 0.2f);
        yield return new WaitForSeconds(0.5f);

        // ���� ���� ���� �ִϸ��̼� ���� ������ ������
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        // �ִϸ��̼��� ���� ������ ���
        while (stateInfo.IsName(animName) && stateInfo.normalizedTime < 1.0f)
        {
            yield return null; // ���� �����ӱ��� ���
            stateInfo = anim.GetCurrentAnimatorStateInfo(0); // ���� ������ ����
        }

        // �ִϸ��̼��� ���� �� ������ �ڵ�
        isAnimating.Value = false;
    }


    public void Execute()
    {
        TcpListener server = null;
        while (true)
        {
            try
            {
                // ������ ����� ��Ʈ ��ȣ
                Int32 port = 6346;
                // ���� �ּ�
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");

                // TcpListener ��ü�� �����Ͽ� ���� ����
                server = new TcpListener(localAddr, port);

                // ���� ����
                server.Start();

                // ������ ����ؼ� �޾Ƶ���
                while (true)
                {
                    // Ŭ���̾�Ʈ ������ �񵿱������� ���
                    TcpClient client = server.AcceptTcpClient();

                    // Ŭ���̾�Ʈ�� ����� ó���� Task ����
                    NetworkStream stream = client.GetStream();

                    int i;
                    byte[] bytes = new byte[1024];
                    string data;

                    try
                    {
                        // Ŭ���̾�Ʈ�κ��� ������ ����
                        while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            data = Encoding.UTF8.GetString(bytes, 0, i);

                            // �޽����� ť�� �߰�
                            messageQueue.Add(data);

                            // �޽��� ����
                            byte[] msg = Encoding.UTF8.GetBytes(data);
                            stream.Write(msg, 0, msg.Length);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log($"Exception: {e}");
                    }
                    finally
                    {
                        // ��Ʈ�� �� Ŭ���̾�Ʈ ���� �ݱ�
                        stream.Close();
                        client.Close();
                    }
                }
            }
            catch (SocketException e)
            {
                Debug.Log($"SocketException: {e}");
            }
            finally
            {
                // ������ ����
                server.Stop();
            }
        }
    }
}

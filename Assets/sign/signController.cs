using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using Unity.Jobs;


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
    Animator anim;
    private static BlockingCollection<string> messageQueue = new BlockingCollection<string>();
    ThreadSafeBoolean isAnimating = new ThreadSafeBoolean(false);

    Dictionary<KeyCode, string> mapAlphabetical = new Dictionary<KeyCode, string>
    {
        { KeyCode.R, "giyeok" },
        { KeyCode.S, "nieun" },
        { KeyCode.E, "digeut" },
        { KeyCode.F, "rieul" },
        { KeyCode.A, "mieum" },
        { KeyCode.Q, "bieup" },
        { KeyCode.T, "shiot" },
        { KeyCode.D, "ieung" },
        { KeyCode.W, "jieut" },
        { KeyCode.C, "chieuch" },
        { KeyCode.Z, "kieuk" },
        { KeyCode.X, "tieut" },
        { KeyCode.V, "pieup" },
        { KeyCode.G, "hieu" },
        { KeyCode.K, "a" },
        { KeyCode.I, "ya" },
        { KeyCode.J, "eo" },
        { KeyCode.U, "yeo" },
        { KeyCode.H, "o" },
        { KeyCode.Y, "yo" },
        { KeyCode.N, "u" },
        { KeyCode.B, "yu" },
        { KeyCode.M, "eu" },
        { KeyCode.L, "i" },
        { KeyCode.O, "ae" },
        { KeyCode.P, "e" },
    };

    Dictionary<string, string> mapPhrase = new Dictionary<string, string>
    {
        { "�ȳ�", "hello" },
        { "��", "me" },
        { "��", "me" },
        { "�̸�", "name" },
        { "�̴�", "is" },
    };

    // Start is called before the first frame update
    void Start()
    {
        anim = gameObject.GetComponent<Animator>();
        SocketThread myJob = new SocketThread();
        myJob.Schedule();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isAnimating.Value)
        {
            foreach (var (key, action) in mapAlphabetical)
            {
                if (Input.GetKeyDown(key))
                {
                    isAnimating.Value = true;
                    StartCoroutine(PlayAnimationAndWait(action));
                }
            }
            if (messageQueue.Count > 0)
            {
                string action = messageQueue.Take();
                if (mapPhrase.ContainsKey(action))
                {
                    isAnimating.Value = true;
                    StartCoroutine(PlayAnimationAndWait(mapPhrase[action]));
                }
            }
        }
    }

    private IEnumerator PlayAnimationAndWait(string animName)
    {
        // �ִϸ��̼��� ũ�ν����̵�� ����
        anim.CrossFade(animName, 0.2f);
        yield return new WaitForSeconds(0.2f);

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

    struct SocketThread : IJob
    {
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
                    Console.WriteLine("Server started...");

                    // ������ ����ؼ� �޾Ƶ���
                    while (true)
                    {
                        Console.WriteLine("Waiting for a connection...");

                        // Ŭ���̾�Ʈ ������ �񵿱������� ���
                        TcpClient client = server.AcceptTcpClient();
                        Console.WriteLine("Connected!");

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
                                Console.WriteLine("Received: {0}", data);

                                // �޽����� ť�� �߰�
                                messageQueue.Add(data);

                                // ���� ����
                                byte[] msg = Encoding.UTF8.GetBytes(data);
                                stream.Write(msg, 0, msg.Length);
                                Console.WriteLine("Sent: {0}", data);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Exception: {0}", e);
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
                    Console.WriteLine("SocketException: {0}", e);
                }
                finally
                {
                    // ������ ����
                    server.Stop();
                }
            }
        }
    }
}

//#define ENABLE_TEST_SROPTIONS

using System;
using System.ComponentModel;
using System.Diagnostics;
using SRDebugger;
using SRDebugger.Services;
using SRF;
using SRF.Service;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public partial class SROptions
{
    // Uncomment the #define at the top of file to enable test options

#if ENABLE_TEST_SROPTIONS

    public enum TestValues
    {
        TestValue1,
        TestValue2,
        TestValue3LongerThisTime
    }

    private bool _testBoolean = true;
    private string _testString = "Test String Value";
    private short _testShort = -10;
    private byte _testByte = 2;
    private int _testInt = 30000;
    private double _testDouble = 406002020d;
    private float _testFloat = 0.1f;
    private sbyte _testSByte = -10;
    private uint _testUInt = 32450;
    private TestValues _testEnum;
    private float _test01Range;
    private float _testFractionIncrement;
    private int _testLargeIncrement;

    [Category("Test")]
    public float TestFloat
    {
        get { return _testFloat; }
        set
        {
            OnValueChanged("TestFloat", value);
            _testFloat = value;
        }
    }

    [Category("Test")]
    public double TestDouble
    {
        get { return _testDouble; }
        set
        {
            OnValueChanged("TestDouble", value);
            _testDouble = value;
        }
    }

    [Category("Test")]
    public int TestInt
    {
        get { return _testInt; }
        set
        {
            OnValueChanged("TestInt", value);
            _testInt = value;
        }
    }

    [Category("Test")]
    public byte TestByte
    {
        get { return _testByte; }
        set
        {
            OnValueChanged("TestByte", value);
            _testByte = value;
        }
    }

    [Category("Test")]
    public short TestShort
    {
        get { return _testShort; }
        set
        {
            OnValueChanged("TestShort", value);
            _testShort = value;
        }
    }

    [Category("Test")]
    public string TestString
    {
        get { return _testString; }
        set
        {
            OnValueChanged("TestString", value);
            _testString = value;
        }
    }

    [Category("Test")]
    public bool TestBoolean
    {
        get { return _testBoolean; }
        set
        {
            OnValueChanged("TestBoolean", value);
            _testBoolean = value;
        }
    }

    [Category("Test")]
    public TestValues TestEnum
    {
        get { return _testEnum; }
        set
        {
            OnValueChanged("TestEnum", value);
            _testEnum = value;
        }
    }

    [Category("Test")]
    public sbyte TestSByte
    {
        get { return _testSByte; }
        set
        {
            OnValueChanged("TestSByte", value);
            _testSByte = value;
        }
    }

    [Category("Test")]
    public uint TestUInt
    {
        get { return _testUInt; }
        set
        {
            OnValueChanged("TestUInt", value);
            _testUInt = value;
        }
    }

    [Category("Test")]
    [NumberRange(0, 1)]
    public float Test01Range
    {
        get { return _test01Range; }
        set
        {
            OnValueChanged("Test01Range", value);
            _test01Range = value;
        }
    }

    [Category("Test")]
    [Increment(0.2)]
    public float TestFractionIncrement
    {
        get { return _testFractionIncrement; }
        set
        {
            OnValueChanged("TestFractionIncrement", value);
            _testFractionIncrement = value;
        }
    }

    [Category("Test")]
    [Increment(25)]
    public int TestLargeIncrement
    {
        get { return _testLargeIncrement; }
        set
        {
            OnValueChanged("TestLargeIncrement", value);
            _testLargeIncrement = value;
        }
    }

    [Category("Test")]
    public void TestAction()
    {
        Debug.Log("[SRDebug] TestAction() invoked");
    }

    [Category("Test"), DisplayName("Test Action Renamed")]
    public void TestRenamedAction()
    {
        Debug.Log("[SRDebug] TestRenamedAction() invoked");
    }

    private void OnValueChanged(string n, object newValue)
    {
        Debug.Log("[SRDebug] {0} value changed to {1}".Fmt(n, newValue));
        OnPropertyChanged(n);
    }

    [Category("SRDebugger")]
    public PinAlignment TriggerPosition
    {
        get { return SRServiceManager.GetService<IDebugTriggerService>().Position; }
        set { SRServiceManager.GetService<IDebugTriggerService>().Position = value; }
    }

    private static readonly string[] SampleLogs =
    {
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
        "Mauris id mauris interdum tellus luctus posuere.",
        "Donec eget velit nec risus bibendum condimentum ut in velit.",
        "Aenean et augue non eros interdum fringilla.",
        "Nam vulputate justo quis nulla ultricies placerat.",
        "Etiam id libero sed quam elementum suscipit.",
        "Nulla sollicitudin purus nec mauris congue tincidunt.",
        "Nam sit amet neque vestibulum, vehicula lorem sed, ultricies dui.",
        "Aenean a eros fringilla, luctus est et, bibendum lorem.",
        "Integer bibendum metus in lectus finibus sagittis.",
        "Quisque a lacus ac massa interdum sagittis nec id sapien.",
        "Phasellus a ipsum volutpat, lobortis velit eu, consectetur nunc.",
        "Nulla elementum justo malesuada lacus mollis lobortis.",
        "Nullam sodales nisi vitae tortor lacinia, in pulvinar mauris accumsan.",
        "Nullam maximus dolor suscipit magna lobortis, eu finibus felis ornare.",
        "Sed eget nisl ac lorem eleifend fermentum ac quis nunc.",
        "Fusce vitae sapien quis turpis faucibus aliquet sit amet et risus.",
        "Nunc faucibus arcu ut purus fringilla bibendum.",
        "Phasellus pretium justo vel eros facilisis varius.",
        "In efficitur quam dapibus nulla commodo, in aliquam nulla bibendum."
    };

    private int _consoleTestQuantity = 190;

    [Category("Console Test")]
    public int ConsoleTestQuantity
    {
        get { return _consoleTestQuantity; }
        set { _consoleTestQuantity = value; }
    }

    [Category("Console Test")]
    public void ConsoleTest()
    {
        var sw = new Stopwatch();
        sw.Start();
        for (var i = 0; i < ConsoleTestQuantity; i++)
        {
            var sample = SampleLogs[Random.Range(0, SampleLogs.Length)];

            var mode = Random.Range(0, 3);

            switch (mode)
            {
                case 0:
                    Debug.Log(sample);
                    break;
                case 1:
                    Debug.LogWarning(sample);
                    break;
                case 2:
                    Debug.LogError(sample);
                    break;
            }
        }
        sw.Stop();

        Debug.Log("Posted {0} log messages in {1}s".Fmt(ConsoleTestQuantity, sw.Elapsed.TotalSeconds));
    }

    [Category("Console Test")]
    public void TestThrowException()
    {
        throw new Exception("This is certainly a test.");
    }

    [Category("Console Test")]
    public void TestLogError()
    {
        Debug.LogError("Test Error");
    }

    [Category("Console Test")]
    public void TestLogWarning()
    {
        Debug.LogWarning("Test Warning");
    }

    [Category("Console Test")]
    public void TestLogInfo()
    {
        Debug.Log("Test Info");
    }

    [Category("Console Test")]
    public void TestRichText()
    {
        Debug.Log(
            "<b>Rich text</b> is <i>supported</i> in the <b><i>console</i></b>. <color=#7fc97a>Color tags, too!</color>");
    }

    [Category("Console Test")]
    public void TestLongMessage()
    {
        var m = SampleLogs[0];

        for (var i = 0; i < 2; ++i)
        {
            m = m + m;
        }

        var s = m;

        for (var i = 0; i < 13; ++i)
        {
            s = s + "\n" + m;
        }

        Debug.Log(s);
    }

    [Category("Sorting Test"), Sort(2)]
    public float ShouldAppearLast { get; set; }

    [Category("Sorting Test"), Sort(-1)]
    public float ShouldAppearFirst { get; set; }

    [Category("Sorting Test")]
    public float ShouldAppearMiddle { get; set; }

    private float _updateTest;

    public float UpdateTest
    {
        get { return _updateTest; }
        set
        {
            _updateTest = value;
            OnPropertyChanged("UpdateTest");
        }
    }

    [DisplayName("Modified Name")]
    public float DisplayNameTest { get; set; }

    [Category("Read Only")]
    public bool ReadOnlyBool { get; private set; }

    [Category("Read Only")]
    public float ReadOnlyNumber { get; private set; }

    [Category("Read Only")]
    public string ReadOnlyString { get; private set; }

    [Category("Read Only")]
    public TestValues ReadOnlyEnum { get; private set; }

    [Category("Read Only")]
    public string TestLongReadOnlyString
    {
        get { return "This is a really long string with no reason other than to test long strings."; }
    }

#endif
}

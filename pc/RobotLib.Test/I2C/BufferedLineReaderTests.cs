using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MongooseSoftware.Robotics.RobotLib.I2C
{
    [TestFixture]
    public class BufferedLineReaderTests
    {
        [Test]
        public void ReadLine_ReceiveOneLine()
        {
            MockSocket socket = new MockSocket();
            socket.AddResponse("test\n");
            var reader = new BufferedLineReader(socket.Receive, 20);

            string result = reader.ReadLine();

            Assert.AreEqual("test\n", result);
            Assert.IsTrue(reader.IsEmpty);
            Assert.IsFalse(reader.IsFull);
        }       
        
         [Test]
        public void ReadLine_OneLineInMultiplePieces()
        {
            MockSocket socket = new MockSocket();
            socket.AddResponse("te");
            socket.AddResponse("st\n");
            var reader = new BufferedLineReader(socket.Receive, 20);

            string result = reader.ReadLine();

            Assert.AreEqual("test\n", result);
            Assert.IsTrue(reader.IsEmpty);
            Assert.IsFalse(reader.IsFull);
        }       
        
        [Test]
        public void ReadLine_ReceiveMultipleLinesAtOnce()
        {
            MockSocket socket = new MockSocket();
            socket.AddResponse("test1\ntest2\ntest3\n");
            var reader = new BufferedLineReader(socket.Receive, 20);

            string result = reader.ReadLine();
            Assert.AreEqual("test1\n", result);
            Assert.IsFalse(reader.IsEmpty);
            Assert.IsFalse(reader.IsFull);
            
            result = reader.ReadLine();
            Assert.AreEqual("test2\n", result);
            Assert.IsFalse(reader.IsEmpty);
            Assert.IsFalse(reader.IsFull);

            result = reader.ReadLine();
            Assert.AreEqual("test3\n", result);
            Assert.IsTrue(reader.IsEmpty);
            Assert.IsFalse(reader.IsFull);
        }

        [Test]
        public void ReadLine_WrapAround()
        {
            MockSocket socket = new MockSocket();
            socket.AddResponse("test\n1");
            socket.AddResponse("23");
            socket.AddResponse("45");
            socket.AddResponse("678\n");
            var reader = new BufferedLineReader(socket.Receive, 10);

            string result = reader.ReadLine();
            Assert.AreEqual("test\n", result);
            Assert.IsFalse(reader.IsEmpty);
            Assert.IsFalse(reader.IsFull);

            result = reader.ReadLine();
            Assert.AreEqual("12345678\n", result);
            Assert.IsTrue(reader.IsEmpty);
            Assert.IsFalse(reader.IsFull);
        }
 
        [Test]
        public void ReadLine_FillBuffer()
        {
            MockSocket socket = new MockSocket();
            socket.AddResponse("123456789\n");
            var reader = new BufferedLineReader(socket.Receive, 10);

            string result = reader.ReadLine();

            Assert.AreEqual("123456789\n", result);
            Assert.IsTrue(reader.IsEmpty);
            Assert.IsFalse(reader.IsFull);
        }

        [Test]
        public void ReadLine_WrapAroundAndFillBuffer()
        {
            MockSocket socket = new MockSocket();
            socket.AddResponse("ab\n1");
            socket.AddResponse("234567");
            socket.AddResponse("89\n");
            var reader = new BufferedLineReader(socket.Receive, 10);

            string result = reader.ReadLine();
            Assert.AreEqual("ab\n", result);
            Assert.IsFalse(reader.IsEmpty);
            Assert.IsFalse(reader.IsFull);

            result = reader.ReadLine();

            Assert.AreEqual("123456789\n", result);
            Assert.IsTrue(reader.IsEmpty);
            Assert.IsFalse(reader.IsFull);
        }

        [Test]
        public void ReadLine_ToTheEndOfTheBuffer()
        {
            MockSocket socket = new MockSocket();
            socket.AddResponse("TEST\ntest");
            socket.AddResponse("\n");
            var reader = new BufferedLineReader(socket.Receive, 10);

            string result = reader.ReadLine();
            Assert.AreEqual("TEST\n", result);
            Assert.IsFalse(reader.IsEmpty);
            Assert.IsFalse(reader.IsFull);
            
            result = reader.ReadLine();
            Assert.AreEqual("test\n", result);
            Assert.IsTrue(reader.IsEmpty);
            Assert.IsFalse(reader.IsFull);
        }       
    
        [Test]
        [ExpectedException(typeof(Exception))]
        public void ReadLine_OverflowsBuffer_ThrowsException()
        {
            MockSocket socket = new MockSocket();
            socket.AddResponse("12345");
            socket.AddResponse("67890");
            socket.AddResponse("\n");
            var reader = new BufferedLineReader(socket.Receive, 10);

            string result = reader.ReadLine();
        }

        [Test]
        public void ReadLine_WrapAround2()
        {
            MockSocket socket = new MockSocket();
            socket.AddResponse("test\n1");
            socket.AddResponse("2\n34");
            socket.AddResponse("56\n");

            var reader = new BufferedLineReader(socket.Receive, 10);
            
            string result = reader.ReadLine();
            Assert.AreEqual("test\n", result);
            Assert.IsFalse(reader.IsEmpty);
            Assert.IsFalse(reader.IsFull);
            
            result = reader.ReadLine();
            Assert.AreEqual("12\n", result);
            Assert.IsFalse(reader.IsEmpty);
            Assert.IsFalse(reader.IsFull);            
            
            result = reader.ReadLine();
            Assert.AreEqual("3456\n", result);
            Assert.IsTrue(reader.IsEmpty);
            Assert.IsFalse(reader.IsFull);
        }

        const int RandomSeed = 1;
        const int SendBufferSize = 2000;
        const int MaxLineLength = 10;
        const int CircularBufferSize = 10;

        int nextToSend;
        byte[] sendBuffer = new byte[SendBufferSize];
        Random random;

        private int ReadTestData(byte[] outputBuffer, int index, int count)
        {
            int dataLeftToSend = SendBufferSize - nextToSend;
            if (dataLeftToSend == 0) return 0;

            int maxToSend = Math.Min(dataLeftToSend, count);
            int bytesToSend = (random.Next() % maxToSend) + 1;

            Array.ConstrainedCopy(sendBuffer, nextToSend, outputBuffer, index, bytesToSend);
            nextToSend += bytesToSend;

            return bytesToSend;
        }

        [Test]
        public void ReadLine_RandomTest()
        {

            // Generate some random test data.
            random = new Random(RandomSeed);
            int countTillNextLine = random.Next() % MaxLineLength;
            for (int i = 0; i < SendBufferSize-1; i++)
                if (countTillNextLine-- > 0)
                    sendBuffer[i] = (byte)((random.Next() % 26) + 65);
                else
                {
                    sendBuffer[i] = 10;
                    countTillNextLine = random.Next() % MaxLineLength;
                }
            sendBuffer[SendBufferSize - 1] = 10;

            // Setup a line reader.
            var reader = new BufferedLineReader(ReadTestData, CircularBufferSize);
            int startOfNextExpectedLine = 0, numLines = 0;
            while (true)
            {
                string result = reader.ReadLine();
                if (result == null) break;
                
                Assert.IsTrue(result.EndsWith("\n"));

                int expectedEndOfLine = Array.IndexOf(sendBuffer, (byte)10, startOfNextExpectedLine, SendBufferSize - startOfNextExpectedLine);
                int expectedLength = expectedEndOfLine - startOfNextExpectedLine + 1;
                Assert.AreEqual(expectedLength, result.Length);

                string expectedResult = ASCIIEncoding.ASCII.GetString(sendBuffer, startOfNextExpectedLine, expectedEndOfLine - startOfNextExpectedLine + 1);
                Assert.AreEqual(expectedResult, result);

                startOfNextExpectedLine += expectedLength;
                numLines++;
            }

            Assert.IsTrue(startOfNextExpectedLine == SendBufferSize);
        }

    }

    public class MockSocket
    {
        public MockSocket()
        {
            responses = new List<string>();
        }

        public void AddResponse(string s)
        {
            responses.Add(s);
        }

        public int Receive(byte[] buffer, int index, int count)
        {
            byte[] bytes = ASCIIEncoding.ASCII.GetBytes(responses[currentResponse]);
            if (bytes.Length > count) throw new ApplicationException("Bad test data");
            Array.Copy(bytes, 0, buffer, index, bytes.Length);
            currentResponse++;
            return bytes.Length;
        }

        int currentResponse;
        List<string> responses;
    }
}

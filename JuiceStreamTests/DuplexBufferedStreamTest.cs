﻿using JuiceStream;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JuiceStreamTests
{
    [TestFixture]
    public class DuplexBufferedStreamTest
    {
        [Test]
        public void CommonOperations()
        {
            TestUtils.TestCommonOperations(inner => new DuplexBufferedStream(inner), bytes => bytes);
        }
    }
}

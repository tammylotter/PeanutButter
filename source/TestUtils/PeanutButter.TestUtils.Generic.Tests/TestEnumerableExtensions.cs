﻿using System.Collections.Generic;
using NUnit.Framework;

namespace PeanutButter.TestUtils.Generic.Tests
{
    public class TestEnumerableExtensions
    {
        private class IntWrapper
        {
            public static IntWrapper For(int i)
            {
                return new IntWrapper(i);
            }
            public IntWrapper(int i)
            {
                Value = i;
            }

            public int Value { get; private set; }
        }

        [Test]
        public void ShouldMatchDataIn_OperatingOnCollection_WhenComparisonCollectionIsDifferentSize_ShouldThrow()
        {
            //---------------Set up test pack-------------------
            var left = new[] {IntWrapper.For(1), IntWrapper.For(2)};
            var right = new[] {IntWrapper.For(1) };

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            Assert.Throws<AssertionException>(() => left.ShouldMatchDataIn(right));
            Assert.Throws<AssertionException>(() => right.ShouldMatchDataIn(left));

            //---------------Test Result -----------------------
        }

        [Test]
        public void IsEquivalentTo_GivenTwoNulls_ShouldReturnTrue()
        {
            //---------------Set up test pack-------------------
            IEnumerable<string> left = null;
            IEnumerable<string> right = null;

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = left.IsEquivalentTo(right);

            //---------------Test Result -----------------------
            Assert.IsTrue(result);
        }

        [Test]
        public void IsEquivalentTo_GivenOneNull_ShouldReturnTrue()
        {
            //---------------Set up test pack-------------------
            var left = new string[] {};
            IEnumerable<string> right = null;

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result1 = left.IsEquivalentTo(right);
            var result2 = right.IsEquivalentTo(left);

            //---------------Test Result -----------------------
            Assert.IsFalse(result1);
            Assert.IsFalse(result2);
        }

        [Test]
        public void IsEquivalentTo_GivenTwoEmptyCollections_ShouldReturnTrue()
        {
            //---------------Set up test pack-------------------
            var left = new string[] {};
            var right = new string[] {};

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result = left.IsEquivalentTo(right);

            //---------------Test Result -----------------------
            Assert.IsTrue(result);
        }

        [Test]
        public void IsEquivalentTo_GivenOneCollectionWithOneItem_AndAnotherWithNoItems_ShouldReturnFalse()
        {
            //---------------Set up test pack-------------------
            var left = new[] {1};
            var right = new int[] {};

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result1 = left.IsEquivalentTo(right);
            var result2 = right.IsEquivalentTo(left);

            //---------------Test Result -----------------------
            Assert.IsFalse(result1);
            Assert.IsFalse(result2);
        }

        [Test]
        public void IsEquivalentTo_GivenMisMatchingCollectionsOfSameLength_ShouldReturnFalse()
        {
            //---------------Set up test pack-------------------
            var left = new[] {true};
            var right = new[] {false};

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result1 = left.IsEquivalentTo(right);
            var result2 = right.IsEquivalentTo(left);

            //---------------Test Result -----------------------
            Assert.IsFalse(result1);
            Assert.IsFalse(result2);
        }

        [Test]
        public void IsEquivalentTo_GivenMisMatchingCollectionsOfDifferentLength_ShouldReturnFalse()
        {
            //---------------Set up test pack-------------------
            var left = new[] {true};
            var right = new[] {true, false};

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result1 = left.IsEquivalentTo(right);
            var result2 = right.IsEquivalentTo(left);

            //---------------Test Result -----------------------
            Assert.IsFalse(result1);
            Assert.IsFalse(result2);
        }

        [Test]
        public void IsEquivalentTo_GivenMatchingCollections_ShouldReturnTrue()
        {
            //---------------Set up test pack-------------------
            var left = new[] {1.2, 3.4};
            var right = new[] {1.2, 3.4};

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            var result1 = left.IsEquivalentTo(right);
            var result2 = right.IsEquivalentTo(left);

            //---------------Test Result -----------------------
            Assert.IsTrue(result1);
            Assert.IsTrue(result2);
        }

    }
}
// Copyright 2009-2012 Josh Close
// This file is a part of CsvHelper and is licensed under the MS-PL
// See LICENSE.txt for details or visit http://www.opensource.org/licenses/ms-pl.html
// http://csvhelper.com
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper.Configuration;
using CsvHelper.Tests.Mocks;
#if WINRT_4_5
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace CsvHelper.Tests
{
	[TestClass]
	public class CsvReaderMappingTests
	{
		[TestMethod]
		public void ReadMultipleNamesTest()
		{
			var data = new List<string[]>
			{
				new[] { "int2", "string3" },
				new[] { "1", "one" },
				new[] { "2", "two" },
				null
			};

			var queue = new Queue<string[]>( data );
			var parserMock = new ParserMock( queue );

			var csvReader = new CsvReader( parserMock );
			csvReader.Configuration.AttributeMapping<MultipleNamesAttributeClass>();

			var records = csvReader.GetRecords<MultipleNamesAttributeClass>().ToList();

			Assert.IsNotNull( records );
			Assert.AreEqual( 2, records.Count );
			Assert.AreEqual( 1, records[0].IntColumn );
			Assert.AreEqual( "one", records[0].StringColumn );
			Assert.AreEqual( 2, records[1].IntColumn );
			Assert.AreEqual( "two", records[1].StringColumn );
		}

		[TestMethod]
		public void ConstructUsingTest()
		{
			var queue = new Queue<string[]>();
			queue.Enqueue( new[] { "1" } );
			var parserMock = new ParserMock( queue );

			var csvReader = new CsvReader( parserMock );
			csvReader.Configuration.HasHeaderRecord = false;
			csvReader.Configuration.ClassMapping<ConstructorMappingClassMap>();

			csvReader.Read();
			var record = csvReader.GetRecord<ConstructorMappingClass>();

			Assert.AreEqual( "one", record.StringColumn );
		}

		[TestMethod]
		public void ConvertUsingTest()
		{
			var queue = new Queue<string[]>();
			queue.Enqueue( new[] { "1", "2" } );
			queue.Enqueue( new[] { "3", "4" } );
			queue.Enqueue( null );

			var parserMock = new ParserMock( queue );

			var csvReader = new CsvReader( parserMock );
			csvReader.Configuration.HasHeaderRecord = false;
			csvReader.Configuration.ClassMapping<ConvertUsingMap>();

			var records = csvReader.GetRecords<TestClass>().ToList();

			Assert.IsNotNull( records );
			Assert.AreEqual( 2, records.Count );
			Assert.AreEqual( 3, records[0].IntColumn );
			Assert.AreEqual( 7, records[1].IntColumn );
		}

		[TestMethod]
		public void ConvertUsingBlockTest()
		{
			var queue = new Queue<string[]>();
			queue.Enqueue( new[] { "1", "2" } );
			queue.Enqueue( new[] { "3", "4" } );
			queue.Enqueue( null );

			var parserMock = new ParserMock( queue );

			var csvReader = new CsvReader( parserMock );
			csvReader.Configuration.HasHeaderRecord = false;
			csvReader.Configuration.ClassMapping<ConvertUsingBlockMap>();

			var records = csvReader.GetRecords<TestClass>().ToList();

			Assert.IsNotNull( records );
			Assert.AreEqual( 2, records.Count );
			Assert.AreEqual( 3, records[0].IntColumn );
			Assert.AreEqual( 7, records[1].IntColumn );
		}

		[TestMethod]
		public void ReaderCacheTest()
		{
			var queue = new Queue<string[]>();
			queue.Enqueue( new[] { "2", "3", "4" } );
			queue.Enqueue( new[] { "3", "1", "2" } );
			queue.Enqueue( null );

			var parserMock = new ParserMock( queue );

			var csvReader = new CsvReader( parserMock );
			csvReader.Configuration.HasHeaderRecord = false;
			csvReader.Configuration.ClassMapping<CacheMap>();

			var records = csvReader.GetRecords<TwoIntClass>().ToList();

			Assert.IsNotNull( records );
			Assert.AreEqual( 2, records.Count );
			Assert.AreEqual( 6, records[0].Int1Column );
			Assert.AreEqual( 8, records[0].Int2Column );

			// Make sure the cache has been reset on
			// for the second row.
			Assert.AreEqual( 3, records[1].Int1Column );
			Assert.AreEqual( 6, records[1].Int2Column );
		}

		private class TestClass
		{
			public int IntColumn { get; set; }
		}

		private class TwoIntClass
		{
			public int Int1Column { get; set; }

			public int Int2Column { get; set; }
		}

		private class MultipleNamesAttributeClass
		{
			[CsvField( Names = new[] { "int1", "int2", "int3" } )]
			public int IntColumn { get; set; }

			[CsvField( Names = new[] { "string1", "string2", "string3" } )]
			public string StringColumn { get; set; }
		}

		private class ConstructorMappingClass
		{
			public int IntColumn { get; set; }

			public string StringColumn { get; set; }

			public ConstructorMappingClass( string stringColumn )
			{
				StringColumn = stringColumn;
			}
		}

		private sealed class ConstructorMappingClassMap : CsvClassMap<ConstructorMappingClass>
		{
			public ConstructorMappingClassMap()
			{
				ConstructUsing( () => new ConstructorMappingClass( "one" ) );
				Map( m => m.IntColumn ).Index( 0 );
			}
		}

		private sealed class ConvertUsingMap : CsvClassMap<TestClass>
		{
			public ConvertUsingMap()
			{
				Map( m => m.IntColumn ).ConvertUsing( row => row.GetField<int>( 0 ) + row.GetField<int>( 1 ) );
			}
		}

		private sealed class ConvertUsingBlockMap : CsvClassMap<TestClass>
		{
			public ConvertUsingBlockMap()
			{
				Map( m => m.IntColumn ).ConvertUsing( row =>
				{
					var x = row.GetField<int>( 0 );
					var y = row.GetField<int>( 1 );
					return x + y;
				} );
			}
		}

		private sealed  class CacheMap : CsvClassMap<TwoIntClass>
		{
			public CacheMap()
			{
				Map( m => m.Int1Column ).ConvertUsing( row =>
				{
					var multiplier = GetMultiplier( row );
					return multiplier * row.GetField<int>( 1 );
				} );

				Map( m => m.Int2Column ).ConvertUsing( row =>
				{
					var multiplier = GetMultiplier( row );
					return multiplier * row.GetField<int>( 2 );
				} );
			}

			private int GetMultiplier( ICsvReaderRow row )
			{
				object multiplier;
				if( !row.Cache.TryGetValue( "multiplier", out multiplier ) )
				{
					multiplier = row.GetField<int>( 0 );
					row.Cache["multiplier"] = multiplier;
				}
				return (int)multiplier;
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Parquet.File;
using Parquet.Serialization;
using Xunit;

namespace Parquet.Test.Reader;

/// <summary>
/// Confirms DELTA_BINARY_PACKED logical types over INT32/INT64 (timestamps, dates, decimals, etc.) decode
/// identically to PLAIN via <see cref="DataColumnReader.DecodeDeltaBinaryPackedViaPlain"/>.
/// </summary>
public class DeltaBinaryPackedLogicalTypesTest : TestBase {

    //We have three files with the exact same data. One is PLAIN encoded,
    //the others delta encoded with v1 and v2 data pages.
    private const string PlainFile = "delta_binary_packed_logical.plain.parquet";

    [Theory]
    [InlineData("delta_binary_packed_logical.delta.v1.parquet")]
    [InlineData("delta_binary_packed_logical.delta.v2.parquet")]
    public async Task Delta_encoded_logical_types_match_plain(string deltaFile) {
        await using Stream plain = OpenTestFile(PlainFile);
        await using Stream delta = OpenTestFile(deltaFile);

        IList<Dictionary<string, object>> expected = (await ParquetSerializer.DeserializeUntypedAsync(plain)).Data;
        IList<Dictionary<string, object>> actual = (await ParquetSerializer.DeserializeUntypedAsync(delta)).Data;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task Delta_encoded_timestamp_values_are_correct() {
        await using Stream s = OpenTestFile("delta_binary_packed_logical.delta.v1.parquet");
        IList<Dictionary<string, object>> rows = (await ParquetSerializer.DeserializeUntypedAsync(s)).Data;

        Assert.Equal(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), rows[0]["ts_us"]);
        Assert.Equal(new DateTime(2020, 1, 2, 1, 41, 3, 307, 377, DateTimeKind.Utc), rows[2499]["ts_us"]);

        Assert.False(rows[0].ContainsKey("ts_nullable"));
        Assert.Equal(rows[1]["ts_us"], rows[1]["ts_nullable"]);
        Assert.False(rows[7].ContainsKey("ts_nullable"));
    }
}

namespace WordSorter.ExternalMerge;

public class ExternalMergeSorter
{
    private long _maxUnsortedRows;
    private double _totalFilesToMerge;
    private int _mergeFilesProcessed;
    private readonly ExternalMergeSorterOptions _options;
    private const string UnsortedFileExtension = ".unsorted";
    private const string SortedFileExtension = ".sorted";
    private const string TempFileExtension = ".tmp";

    public ExternalMergeSorter() : this(new ExternalMergeSorterOptions())
    {
    }

    public ExternalMergeSorter(ExternalMergeSorterOptions options)
    {
        _totalFilesToMerge = 0;
        _mergeFilesProcessed = 0;
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task Sort(string inputFilename, string outputFilename, CancellationToken cancellationToken)
    {
        var inputFileInfo = new FileInfo(inputFilename);
        await using var output = File.Create(outputFilename);
        if (inputFileInfo.Length <= _options.Split.FileSize)
        {
            _options.Split.ProgressHandler?.Report(1);
            await SortSingleFile(inputFilename, output);
            _options.Sort.ProgressHandler?.Report(1);
            _options.Merge.ProgressHandler?.Report(1);
            return;
        }

        await using var input = File.Open(inputFilename, FileMode.Open);
        var files = await SplitFile(input, cancellationToken);

        var sortedFiles = await SortFiles(files);

        var done = false;
        var size = _options.Merge.ChunkFilesStep;
        _totalFilesToMerge = sortedFiles.Count;
        var result = sortedFiles.Count / size;

        while (!done)
        {
            if (result <= 0)
            {
                done = true;
            }

            _totalFilesToMerge += result;
            result /= size;
        }

        await MergeFiles(sortedFiles, output, cancellationToken);
    }

    private async Task<IReadOnlyCollection<string>> SplitFile(Stream sourceStream, CancellationToken cancellationToken)
    {
        var fileSize = _options.Split.FileSize;
        var buffer = new byte[fileSize];
        var extraBuffer = new List<byte>();
        var filenames = new List<string>();
        var totalFiles = Math.Ceiling(sourceStream.Length / (double)_options.Split.FileSize);

        await using (sourceStream)
        {
            var currentFile = 0L;
            while (sourceStream.Position < sourceStream.Length)
            {
                var totalRows = 0;
                var runBytesRead = 0;
                while (runBytesRead < fileSize)
                {
                    var value = sourceStream.ReadByte();
                    if (value == -1)
                    {
                        break;
                    }

                    var @byte = (byte)value;
                    buffer[runBytesRead] = @byte;
                    runBytesRead++;
                    if (@byte == _options.Split.NewLineSeparator)
                    {
                        totalRows++;
                    }
                }

                var extraByte = buffer[fileSize - 1];

                while (extraByte != _options.Split.NewLineSeparator)
                {
                    var flag = sourceStream.ReadByte();
                    if (flag == -1)
                    {
                        break;
                    }

                    extraByte = (byte)flag;
                    extraBuffer.Add(extraByte);
                }

                var filename = $"{++currentFile}.unsorted";
                await using var unsortedFile = File.Create(filename);
                await unsortedFile.WriteAsync(buffer.AsMemory(0, runBytesRead), cancellationToken);
                if (extraBuffer.Count > 0)
                {
                    totalRows++;
                    await unsortedFile.WriteAsync(extraBuffer.ToArray(), 0, extraBuffer.Count, cancellationToken);
                }

                if (totalRows > _maxUnsortedRows)
                {
                    _maxUnsortedRows = totalRows;
                }

                _options.Split.ProgressHandler?.Report(currentFile / totalFiles);
                filenames.Add(filename);
                extraBuffer.Clear();
            }
            _options.Split.ProgressHandler?.Report(1);
            return filenames;
        }
    }

    private async Task<IReadOnlyList<string>> SortFiles(IReadOnlyCollection<string> unsortedFiles)
    {
        double totalFiles = unsortedFiles.Count;

        var filesMapping = new Dictionary<string, string>();
        foreach (var unsortedFile in unsortedFiles)
        {
            var sortedFilename = unsortedFile.Replace(UnsortedFileExtension, SortedFileExtension);
            filesMapping.Add(unsortedFile, sortedFilename);
        }

        var poolOfArrays = new ArraysPool<string>(_maxUnsortedRows);
        var sortedFiles = 0;
        await unsortedFiles.ForEachAsync(_options.Sort.MaxNumberOfThreads, async unsortedFile =>
        {
            var array = poolOfArrays.Get();
            var sortedFilename = filesMapping[unsortedFile];
            await SortFile(File.OpenRead(unsortedFile), File.OpenWrite(sortedFilename), array);
            File.Delete(unsortedFile);
            poolOfArrays.Put(array);
            var sf = Interlocked.Increment(ref sortedFiles);
            _options.Sort.ProgressHandler?.Report(sf / totalFiles);
        });

        return unsortedFiles.Select(unsortedFile => filesMapping[unsortedFile]).ToList();
    }

    private async Task SortSingleFile(string inputFilename, Stream target)
    {
        var unsortedLines = await File.ReadAllLinesAsync(inputFilename);
        await SortArray(unsortedLines, target);
    }

    private async Task SortFile(Stream unsortedFile, Stream target, string[] unsortedRows)
    {
        using var streamReader = new StreamReader(unsortedFile, bufferSize: _options.Sort.InputBufferSize);
        var counter = 0;
        while (!streamReader.EndOfStream)
        {
            unsortedRows[counter++] = (await streamReader.ReadLineAsync())!;
        }

        await SortArray(unsortedRows, target);
        Array.Clear(unsortedRows, 0, unsortedRows.Length);
    }

    private async Task SortArray(string[] unsortedRows, Stream target)
    {
        Array.Sort(unsortedRows, _options.Sort.Comparer);
        await using var streamWriter = new StreamWriter(target, bufferSize: _options.Sort.OutputBufferSize);
        foreach (var row in unsortedRows.Where(x => x is not null))
        {
            await streamWriter.WriteLineAsync(row);
        }
    }

    private async Task MergeFiles(IReadOnlyList<string> sortedFiles, Stream target, CancellationToken cancellationToken)
    {
        var done = false;
        while (!done)
        {
            var finalRun = sortedFiles.Count <= _options.Merge.ChunkFilesStep;
            if (finalRun)
            {
                await Merge(sortedFiles, target, cancellationToken);
                _options.Merge.ProgressHandler?.Report(1);
                return;
            }

            var runs = sortedFiles.Chunk(_options.Merge.ChunkFilesStep);
            var chunkCounter = 0;
            foreach (var files in runs)
            {
                var outputFilename = $"{++chunkCounter}{SortedFileExtension}{TempFileExtension}";
                if (files.Length == 1)
                {
                    OverwriteTempFile(files.First(), outputFilename);
                    continue;
                }

                var outputStream = File.OpenWrite(outputFilename);
                await Merge(files, outputStream, cancellationToken);
                OverwriteTempFile(outputFilename, outputFilename);

                void OverwriteTempFile(string from, string to)
                {
                    File.Move(from, to.Replace(TempFileExtension, string.Empty), true);
                }
            }

            sortedFiles = Directory.GetFiles(".", $"*{SortedFileExtension}")
                .OrderBy(x =>
                {
                    var filename = Path.GetFileNameWithoutExtension(x);
                    return int.Parse(filename);
                }).ToArray();

            if (sortedFiles.Count <= 1)
            {
                done = true;
            }
        }
    }

    private async Task Merge(IReadOnlyList<string> filesToMerge, Stream outputStream, CancellationToken cancellationToken)
    {
        var (streamReaders, rows) = await InitializeStreamReaders(filesToMerge);
        var finishedStreamReaders = new List<int>(streamReaders.Length);
        var done = false;
        await using var outputWriter = new StreamWriter(outputStream, bufferSize: _options.Merge.OutputBufferSize);

        while (!done)
        {
            rows.Sort((row1, row2) => _options.Sort.Comparer.Compare(row1.Value, row2.Value));
            var valueToWrite = rows[0].Value;
            var streamReaderIndex = rows[0].StreamReader;
            await outputWriter.WriteLineAsync(valueToWrite.AsMemory(), cancellationToken);

            if (streamReaders[streamReaderIndex].EndOfStream)
            {
                var indexToRemove = rows.FindIndex(x => x.StreamReader == streamReaderIndex);
                rows.RemoveAt(indexToRemove);
                finishedStreamReaders.Add(streamReaderIndex);
                done = finishedStreamReaders.Count == streamReaders.Length;
                _options.Merge.ProgressHandler?.Report(++_mergeFilesProcessed / _totalFilesToMerge);
                continue;
            }

            var value = await streamReaders[streamReaderIndex].ReadLineAsync(cancellationToken);
            rows[0] = new RowStreamValue { Value = value!, StreamReader = streamReaderIndex };
        }

        CleanupRun(streamReaders, filesToMerge);
    }

    private async Task<(StreamReader[] StreamReaders, List<RowStreamValue> rows)> InitializeStreamReaders(IReadOnlyList<string> sortedFiles)
    {
        var streamReaders = new StreamReader[sortedFiles.Count];
        var rows = new List<RowStreamValue>();
        for (var i = 0; i < sortedFiles.Count; i++)
        {
            var sortedFileStream = File.OpenRead(sortedFiles[i]);
            streamReaders[i] = new StreamReader(sortedFileStream, bufferSize: _options.Merge.InputBufferSize);
            var value = await streamReaders[i].ReadLineAsync();
            var row = new RowStreamValue
            {
                Value = value!,
                StreamReader = i
            };
            rows.Add(row);
        }

        return (streamReaders, rows);
    }

    private static void CleanupRun(IReadOnlyList<StreamReader> streamReaders, IReadOnlyList<string> filesToMerge)
    {
        for (var i = 0; i < streamReaders.Count; i++)
        {
            streamReaders[i].Dispose();
            var temporaryFilename = $"{filesToMerge[i]}.removal";
            File.Move(filesToMerge[i], temporaryFilename);
            File.Delete(temporaryFilename);
        }
    }
}

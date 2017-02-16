-- number of tests in number of failed tests in control queue
select (select count(TestResults.TestName)
    from TestResults
        JOIN Jobs ON (TestResults.JobPath = Jobs.JobPath)
        JOIN SnapSubmissions on (SnapSubmissions.JobsPath = jobs.SubmissionParent)
    where SnapSubmissions.SnapQueueName = 'SqlStudio_control'
        AND TestResults.Outcome = 'Failed') as FailedTestsInControlQueue,
    (select count(TestResults.TestName)
    from TestResults
        JOIN Jobs ON (TestResults.JobPath = Jobs.JobPath)
        JOIN SnapSubmissions on (SnapSubmissions.JobsPath = jobs.SubmissionParent)
    where SnapSubmissions.SnapQueueName = 'SqlStudio_control') as NumberOfTestsInControlQueue
	   --  (FailedTestsInControlQueue / NumberOfTestsInControlQueue) as PercentageOfTestsFailed
//using Quartz;
//using WhatsappAutomation;
//using WhatsappAutomation.DataContext;
//using WhatsappAutomation.Jobs;
//using WhatsappAutomation.Service;

//var builder = Host.CreateApplicationBuilder(args);
//builder.Services.AddHostedService<Worker>();
//builder.Services.AddDbContext<MainDataContext>();
//builder.Services.AddScoped<GetSqlData>();
//builder.Services.AddQuartz(q =>
//{
//    var jobKey = new JobKey("DailyInvoice");

//    q.AddJob<DailyInovice>(x =>
//        x.WithIdentity(jobKey));


//    q.AddTrigger(x =>
//        x.ForJob(jobKey)
//        .WithIdentity("DailyInvoice-trigger")
//        .WithCronSchedule("0 0 9 ? * *")); // every day 9 AM
//});


//builder.Services.AddQuartzHostedService();


//var host = builder.Build();
//host.Run();

using DevExpress.XtraReports.Services;
using DevExpress.XtraReports.Summary.Native;
using Quartz;
using TexERP.Commons;
using WhatsappAutomation;
using WhatsappAutomation.Commons;
using WhatsappAutomation.DataContext;
using WhatsappAutomation.Jobs;
using WhatsappAutomation.ReportControllers;
using WhatsappAutomation.Service;
using WhatsappAutomation.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddScoped<WhatsappService>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddScoped<IReportProvider, ReportByNameService>();
builder.Services.AddScoped<ReportServiceDeskTop>();
builder.Services.AddDbContext<MainDataContext>();

builder.Services.AddScoped<GetSqlData>();
builder.Services.AddScoped<EmailService>();


//builder.Services.AddQuartz(q =>
//{
//    var jobKey = new JobKey("WeaklyOutStanding");

//    q.AddJob<WeaklyDebOuts>(x =>
//        x.WithIdentity(jobKey));

//    q.AddTrigger(x =>
//        x.ForJob(jobKey)
//        .WithIdentity("WeaklyOutStanding-trigger")
//        .WithCronSchedule("0 0/5 * * * ?")); // testing every 10 seconds


//    // Monday Job
//    //var weeklyJob = new JobKey("WeeklyInvoice");

//    //q.AddJob<WeeklyInvoice>(x =>
//    //    x.WithIdentity(weeklyJob));

//    //q.AddTrigger(x =>
//    //    x.ForJob(weeklyJob)
//    //    .WithIdentity("WeeklyReport-trigger")
//    //    .WithCronSchedule("0 37 12 ? * MON"));
//});

builder.Services.AddQuartz(q =>
{
    var DailyInvoicejobKey = new JobKey("DailyInvoice");

    q.AddJob <DailyInvoice>(opts =>
        opts.WithIdentity(DailyInvoicejobKey));

    q.AddTrigger(opts =>
        opts.ForJob(DailyInvoicejobKey)
        .WithIdentity("DailyInvoice-trigger")
        .WithCronSchedule(CommonClass.ReadSetting("QuartzJobs:DailyInvoice:Cron")));

    //---------------------------------------------daily inovice job end---------------------------------------------
    var DailyDebitorOutstanding = new JobKey("DailyDebitorOutstanding");

    q.AddJob<DailyDebitorOutstanding>(opts =>
       opts.WithIdentity(DailyDebitorOutstanding));

    q.AddTrigger(opts =>
        opts.ForJob(DailyDebitorOutstanding)
        .WithIdentity("DailyDebitorOutstanding-trigger")
        .WithCronSchedule(CommonClass.ReadSetting("QuartzJobs:DailyDebOutstanding:Cron")));

    //---------------------------------------------Daily Debitor Outstanding job end---------------------------------------------
    var WeaklyDebitorOutsandingJobKey = new JobKey("WeaklyDebitorOutsanding");

    q.AddJob<WeaklyDebitorOutsanding>(opts =>
        opts.WithIdentity(WeaklyDebitorOutsandingJobKey));

    q.AddTrigger(opts =>
        opts.ForJob(WeaklyDebitorOutsandingJobKey)
        .WithIdentity("WeaklyDebitorOutsanding-trigger")
        .WithCronSchedule(CommonClass.ReadSetting("QuartzJobs:WeeklyDebOutstanding:Cron")));

    //---------------------------------------------Weekly Debitor Outstanding job end---------------------------------------------

    var WeaklyCreditorOutstanding = new JobKey("WeaklyCreditorOutstanding");

    q.AddJob<WeaklyCreditorOutstanding>(opts =>
        opts.WithIdentity(WeaklyCreditorOutstanding));

    q.AddTrigger(opts =>
        opts.ForJob(WeaklyCreditorOutstanding)
        .WithIdentity("WeaklyCreditorOutstanding-trigger")
        .WithCronSchedule(CommonClass.ReadSetting("QuartzJobs:WeeklyCredOutstanding:Cron")));

    //---------------------------------------------Weekly Creditor Outstanding job end---------------------------------------------

});


builder.Services.AddQuartzHostedService();

var host = builder.Build();

host.Run();


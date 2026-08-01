
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
using Microsoft.Extensions.Hosting;


var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "WhatsappAutomation";
});

builder.Services.AddScoped<WhatsappService>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddScoped<IReportProvider, ReportByNameService>();
builder.Services.AddScoped<ReportServiceDeskTop>();
builder.Services.AddDbContext<MainDataContext>();

builder.Services.AddScoped<GetSqlData>();
builder.Services.AddScoped<EmailService>();


builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();//add this line if problem occure then remvoe it

    if (CommonClass.ReadSetting1<bool>("QuartzJobs:GenrateToken:Enable"))
    {

        var GenrateTokenJobKey = new JobKey("GenrateToken");

        q.AddJob<GenrateToken>(opts =>
            opts.WithIdentity(GenrateTokenJobKey));

        q.AddTrigger(opts => opts
    .ForJob(GenrateTokenJobKey)
    .WithIdentity("GenrateToken-startup-trigger")
    .StartNow());

        q.AddTrigger(opts => opts
            .ForJob(GenrateTokenJobKey)
            .WithIdentity("GenrateToken-cron-trigger")
            .WithCronSchedule(CommonClass.ReadSetting("QuartzJobs:GenrateToken:Cron")));
    }

    //---------------------------------------------Genrate Token  end---------------------------------------------
    if (CommonClass.ReadSetting1<bool>("QuartzJobs:DailyInvoice:Enable"))
    {
        var DailyInvoicejobKey = new JobKey("DailyInvoice");

        q.AddJob<DailyInvoice>(opts =>
            opts.WithIdentity(DailyInvoicejobKey));

        q.AddTrigger(opts =>
            opts.ForJob(DailyInvoicejobKey)
            .WithIdentity("DailyInvoice-trigger")
            .WithCronSchedule(CommonClass.ReadSetting("QuartzJobs:DailyInvoice:Cron")));
    }

    //---------------------------------------------daily inovice job end---------------------------------------------
    if (CommonClass.ReadSetting1<bool>("QuartzJobs:DailyDebOutstanding:Enable"))
    {
        var DailyDebitorOutstanding = new JobKey("DailyDebitorOutstanding");

        q.AddJob<DailyDebitorOutstanding>(opts =>
           opts.WithIdentity(DailyDebitorOutstanding));

        q.AddTrigger(opts =>
            opts.ForJob(DailyDebitorOutstanding)
            .WithIdentity("DailyDebitorOutstanding-trigger")
            .WithCronSchedule(CommonClass.ReadSetting("QuartzJobs:DailyDebOutstanding:Cron")));
    }
    //---------------------------------------------Daily Debitor Outstanding job end---------------------------------------------
    if (CommonClass.ReadSetting1<bool>("QuartzJobs:WeeklyDebOutstanding:Enable"))
    {
        var WeaklyDebitorOutsandingJobKey = new JobKey("WeaklyDebitorOutsanding");

        q.AddJob<WeaklyDebitorOutsanding>(opts =>
            opts.WithIdentity(WeaklyDebitorOutsandingJobKey));

        q.AddTrigger(opts =>
            opts.ForJob(WeaklyDebitorOutsandingJobKey)
            .WithIdentity("WeaklyDebitorOutsanding-trigger")
            .WithCronSchedule(CommonClass.ReadSetting("QuartzJobs:WeeklyDebOutstanding:Cron")));
    }

    //---------------------------------------------Weekly Debitor Outstanding job end---------------------------------------------

    if (CommonClass.ReadSetting1<bool>("QuartzJobs:WeeklyCredOutstanding:Enable"))
    {
        var WeaklyCreditorOutstanding = new JobKey("WeaklyCreditorOutstanding");

        q.AddJob<WeaklyCreditorOutstanding>(opts =>
            opts.WithIdentity(WeaklyCreditorOutstanding));

        q.AddTrigger(opts =>
            opts.ForJob(WeaklyCreditorOutstanding)
            .WithIdentity("WeaklyCreditorOutstanding-trigger")
            .WithCronSchedule(CommonClass.ReadSetting("QuartzJobs:WeeklyCredOutstanding:Cron")));
    }

    //---------------------------------------------Weekly Creditor Outstanding job end---------------------------------------------

});


builder.Services.AddQuartzHostedService();

var host = builder.Build();

host.Run();


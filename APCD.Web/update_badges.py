import os, re

files = [
    r"c:\Users\dell\Desktop\NPC\Vijay-Sir\APCDPortal\APCD Portal\APCD.Web\Views\Application\Step2.cshtml",
    r"c:\Users\dell\Desktop\NPC\Vijay-Sir\APCDPortal\APCD Portal\APCD.Web\Views\Application\Step3.cshtml",
    r"c:\Users\dell\Desktop\NPC\Vijay-Sir\APCDPortal\APCD Portal\APCD.Web\Views\Application\Step4.cshtml",
    r"c:\Users\dell\Desktop\NPC\Vijay-Sir\APCDPortal\APCD Portal\APCD.Web\Views\Application\Step5.cshtml"
]

pattern1 = r'(@if \((Model\.Documents|docs) != null && \2\.Any\(d => d\.DocumentType == "([^"]+)"\)\) \{)\s*<span class="badge bg-success-subtle[^>]+>Uploaded</span>\s*\}'
replacement1 = r'''\1
    var tmpDoc = \2.First(d => d.DocumentType == "\3");
    <div class="mt-2"><i class="bi bi-check-circle-fill text-success"></i> <span class="small text-success fw-bold">Uploaded:</span> <a href="@tmpDoc.FilePath" target="_blank" class="small text-decoration-none">@tmpDoc.FileName</a></div>
}'''

pattern2 = r'(@if \((Model\.Documents)\.Any\(d => d\.DocumentType == "?([^"]+)"?\)\) \{)\s*<span class="badge bg-success-subtle[^>]+>Uploaded</span>\s*\}'
replacement2 = r'''\1
    var tmpDoc = \2.First(d => d.DocumentType == "\3");
    <div class="mt-2"><i class="bi bi-check-circle-fill text-success"></i> <span class="small text-success fw-bold">Uploaded:</span> <a href="@tmpDoc.FilePath" target="_blank" class="small text-decoration-none">@tmpDoc.FileName</a></div>
}'''

# In Step5, we have: `Model.Documents.Any(d => d.DocumentType == $"TurnoverCert_{year}")`
# So we need a special pattern for Turnover
pattern_turnover = r'(@if \(Model\.Documents\.Any\(d => d\.DocumentType == \$\"TurnoverCert_\{year\}\"\)\) \{)\s*<span class="badge bg-success-subtle[^>]+>Uploaded</span>\s*\}'
replacement_turnover = r'''\1
    var tmpDoc = Model.Documents.First(d => d.DocumentType == $"TurnoverCert_{year}");
    <div class="mt-2 text-center"><i class="bi bi-check-circle-fill text-success"></i> <div class="small text-success fw-bold">Uploaded:</div> <a href="@tmpDoc.FilePath" target="_blank" class="small text-decoration-none">@tmpDoc.FileName</a></div>
}'''

for fpath in files:
    if os.path.exists(fpath):
        with open(fpath, 'r', encoding='utf-8') as f:
            content = f.read()
            
        new_content = re.sub(pattern1, replacement1, content)
        new_content = re.sub(pattern2, replacement2, new_content)
        new_content = re.sub(pattern_turnover, replacement_turnover, new_content)
        
        if content != new_content:
            with open(fpath, 'w', encoding='utf-8') as f:
                f.write(new_content)
            print(f"Updated {fpath}")
        else:
            print(f"No changes for {fpath}")

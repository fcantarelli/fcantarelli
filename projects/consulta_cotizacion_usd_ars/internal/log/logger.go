package log

import (
    "log"
    "os"
)

func InitLogger() {
    log.SetFlags(log.Ldate | log.Ltime | log.Lshortfile)
    log.SetOutput(os.Stdout)
}

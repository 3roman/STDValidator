package main

import (
	"fmt"
	"github.com/harry1453/go-common-file-dialog/cfd"
	"io/ioutil"
	"path/filepath"
	"regexp"
	"sync"
	"time"
)

func pickFolder() string {
	pickFolderDialog, err := cfd.NewSelectFolderDialog(cfd.DialogConfig{})
	if err != nil {
		fmt.Println(err)
	}
	if err := pickFolderDialog.Show(); err != nil {
		fmt.Println(err)
	}
	result, err := pickFolderDialog.GetResult()
	//if err == cfd.ErrorCancelled {
	//	log.Fatal("Dialog was cancelled by the user.")
	//} else if err != nil {
	//	log.Fatal(err)
	//}

	return result
}

func getAllFile(folderPath string) ([]string, error) {
	// 获取绝对路径
	absDirPath, err := filepath.Abs(folderPath)
	if err != nil {
		return nil, err
	}
	// 遍历当前路径
	files, err := ioutil.ReadDir(absDirPath)
	if err != nil {
		return nil, err
	}

	var fileNames []string
	for _, value := range files {
		// 递归调用
		if value.IsDir() {
			fns, _ := getAllFile(folderPath + `\` + value.Name())
			fileNames = append(fileNames, fns...)
		} else {
			fileNames = append(fileNames, value.Name())
		}
	}

	return fileNames, err
}

func extractString(rawString string, pattern string) string {
	re := regexp.MustCompile(pattern)
	matchedString := re.FindString(rawString)

	return matchedString
}

func workingRoutine(params []string, channelCapacity int) {
	wg := sync.WaitGroup{}
	start := time.Now()

	channel := make(chan string, channelCapacity)
	for _, param := range params {
		// 当channel满了以后，阻塞在这里
		channel <- param
		wg.Add(1)
		go func() {
			defer wg.Done()
			// TODO 核心功能
			time.Sleep(time.Second)
			value := <-channel
			fmt.Println(value)
		}()
	}

	wg.Wait()
	fmt.Printf("Completed with %0.5f s\n", time.Since(start).Seconds())
}

func main() {
	folderPath := pickFolder()
	standardNames, _ := getAllFile(folderPath)
	for idx, value := range standardNames {
		standardNames[idx] = extractString(value, `[\w\d\s-.]+`)
	}
	workingRoutine(standardNames, 60)
}

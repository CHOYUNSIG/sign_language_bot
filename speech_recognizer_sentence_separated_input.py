import speech_recognition as sr
import socket
from konlpy.tag import Okt
from transformers import pipeline

sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
try:
    sock.settimeout(5)
    sock.connect(('127.0.0.1', 6346))
    print("connected")
except:
    print("not connected")
    sock = None


if __name__ == "__main__":
    okt = Okt()
    recognizer = sr.Recognizer()
    recognizer.non_speaking_duration = 0.1
    recognizer.pause_threshold = 0.15
    classifier = pipeline("text-classification", model="gg4ever/intent-classification-korean")

    with sr.Microphone() as source:
        print("Started")
        while True:
            input("녹음하려면 enter를 누르세요.")

            print("녹음중... ")
            audio = recognizer.listen(source, phrase_time_limit=10)
            print("녹음 끝")
            
            print("처리 중... ")
            text = recognizer.recognize_google(audio, language='ko-KR')
            if not text:
                text = ''
            classification = classifier(text)[0]
            stems = okt.morphs(text, stem=True)
            if classification['label'] == 'question':
                stems.append('?')
            print("처리 완료")

            print()
            print("인식된 원문: ", text)
            print("문장 유형: ", classification)
            print("기본형 전환: ", stems)
            print()

            if sock is not None:
                for stem in stems:
                    sock.send(stem.encode("utf-8"))
                    sock.recv(4096)
            

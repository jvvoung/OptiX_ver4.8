#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
OptiX 운영 프로그램 (클라이언트)
OptiX UI 서버와 TCP/IP 통신하여 검사 명령을 전송합니다.
"""

import socket
import time
import json
import sys

class OptiXClient:
    def __init__(self, server_ip="127.0.0.1", server_port=7777):
        """
        OptiX 클라이언트 초기화
        
        Args:
            server_ip (str): OptiX UI 서버 IP 주소
            server_port (int): OptiX UI 서버 포트 번호
        """
        self.server_ip = server_ip
        self.server_port = server_port
        self.client_socket = None
        self.connected = False
        
    def connect(self):
        """OptiX UI 서버에 연결"""
        try:
            print(f"🔌 OptiX UI 서버에 연결 중... ({self.server_ip}:{self.server_port})")
            
            self.client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.client_socket.settimeout(5)  # 5초 타임아웃
            self.client_socket.connect((self.server_ip, self.server_port))
            
            self.connected = True
            print("✅ OptiX UI 서버 연결 성공!")
            return True
            
        except Exception as e:
            print(f"❌ OptiX UI 서버 연결 실패: {e}")
            self.connected = False
            return False
    
    def disconnect(self):
        """OptiX UI 서버 연결 해제"""
        if self.client_socket:
            self.client_socket.close()
            self.connected = False
            print("🔌 OptiX UI 서버 연결 해제")
    
    def send_command(self, command, parameters=None):
        """
        OptiX UI 서버에 명령 전송
        
        Args:
            command (str): 명령어 (예: "TEST_START", "TEST_STOP")
            parameters (dict): 명령 매개변수
            
        Returns:
            str: 서버 응답
        """
        if not self.connected:
            print("❌ 서버에 연결되지 않았습니다.")
            return None
            
        try:
            # 명령 데이터 구성
            data = {
                "command": command,
                "parameters": parameters or {},
                "timestamp": time.time()
            }
            
            # JSON으로 직렬화
            message = json.dumps(data, ensure_ascii=False)
            message_bytes = message.encode('utf-8')
            
            print(f"📤 명령 전송: {command}")
            if parameters:
                print(f"   매개변수: {parameters}")
            
            # 명령 전송
            self.client_socket.send(message_bytes)
            
            # 응답 수신
            response_bytes = self.client_socket.recv(4096)
            response = response_bytes.decode('utf-8')
            
            print(f"📥 서버 응답: {response}")
            return response
            
        except Exception as e:
            print(f"❌ 명령 전송 실패: {e}")
            return None
    
    def test_start(self, test_type="IPVS", zones=None):
        """
        검사 시작 명령 전송
        
        Args:
            test_type (str): 검사 유형 ("IPVS" 또는 "MTP")
            zones (list): 검사할 Zone 목록 (None이면 모든 Zone)
        """
        parameters = {
            "test_type": test_type,
            "zones": zones or []
        }
        
        return self.send_command("TEST_START", parameters)
    
    def test_stop(self):
        """검사 중지 명령 전송"""
        return self.send_command("TEST_STOP")
    
    def get_status(self):
        """상태 조회 명령 전송"""
        return self.send_command("GET_STATUS")
    
    def ping(self):
        """연결 테스트 명령 전송"""
        return self.send_command("PING")

def main():
    """메인 함수"""
    print("🚀 OptiX 운영 프로그램 (클라이언트) 시작")
    print("=" * 50)
    
    # 서버 설정 (OptiX UI 설정에 맞게 수정)
    SERVER_IP = "127.0.0.1"  # OptiX UI 서버 IP
    SERVER_PORT = 7777       # OptiX UI 서버 포트
    
    # 클라이언트 생성
    client = OptiXClient(SERVER_IP, SERVER_PORT)
    
    try:
        # 서버 연결
        if not client.connect():
            print("❌ 서버 연결에 실패했습니다. OptiX UI가 실행 중인지 확인해주세요.")
            return
        
        # 연결 테스트
        print("\n🔍 연결 테스트...")
        response = client.ping()
        if response:
            print("✅ 연결 테스트 성공!")
        else:
            print("❌ 연결 테스트 실패!")
            return
        
        # 상태 조회
        print("\n📊 상태 조회...")
        client.get_status()
        
        # 검사 시작 명령 테스트
        print("\n🧪 IPVS 검사 시작 명령 테스트...")
        client.test_start("IPVS", [1, 2, 3])
        
        # 잠시 대기
        time.sleep(2)
        
        # 검사 중지 명령 테스트
        print("\n⏹️ 검사 중지 명령 테스트...")
        client.test_stop()
        
        # MTP 검사 시작 명령 테스트
        print("\n🔬 MTP 검사 시작 명령 테스트...")
        client.test_start("MTP", [1, 2])
        
        print("\n✅ 모든 테스트 완료!")
        
    except KeyboardInterrupt:
        print("\n⏹️ 사용자에 의해 중단됨")
    except Exception as e:
        print(f"\n❌ 오류 발생: {e}")
    finally:
        # 연결 해제
        client.disconnect()
        print("\n👋 OptiX 운영 프로그램 종료")

if __name__ == "__main__":
    main()

